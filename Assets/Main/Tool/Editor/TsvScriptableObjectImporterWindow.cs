using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class TsvScriptableObjectImporterWindow : EditorWindow
{
    private TsvTable selectedTable;
    private DefaultAsset outputFolder;
    private string[] typeNames = Array.Empty<string>();
    private Type[] scriptableObjectTypes = Array.Empty<Type>();
    private int selectedTypeIndex;
    private int selectedNameColumnIndex;
    private string fallbackAssetNamePrefix = "Row";
    private Vector2 scrollPosition;

    [MenuItem("Tools/TSV/Import Selected Table To ScriptableObjects")]
    private static void OpenWindow()
    {
        TsvScriptableObjectImporterWindow window = GetWindow<TsvScriptableObjectImporterWindow>("TSV Importer");
        window.minSize = new Vector2(460f, 320f);
        window.RefreshTypes();
        window.TryAssignSelection();
    }

    public static void OpenWindow(TsvTable table)
    {
        TsvScriptableObjectImporterWindow window = GetWindow<TsvScriptableObjectImporterWindow>("TSV Importer");
        window.minSize = new Vector2(460f, 320f);
        window.RefreshTypes();
        window.selectedTable = table;
        window.TryAssignSelection();
    }

    private void OnEnable()
    {
        RefreshTypes();
        TryAssignSelection();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("TSV To ScriptableObject", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(false)) {
            selectedTable = (TsvTable)EditorGUILayout.ObjectField("TSV Table", selectedTable, typeof(TsvTable), false);
            outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);
        }

        if (scriptableObjectTypes.Length == 0) {
            EditorGUILayout.HelpBox("No concrete ScriptableObject types were found in this project.", MessageType.Warning);
            return;
        }

        selectedTypeIndex = EditorGUILayout.Popup("Target Type", selectedTypeIndex, typeNames);

        string[] columnNames = BuildColumnNames();
        selectedNameColumnIndex = Mathf.Clamp(selectedNameColumnIndex, 0, Mathf.Max(columnNames.Length - 1, 0));
        selectedNameColumnIndex = EditorGUILayout.Popup("Asset Name Column", selectedNameColumnIndex, columnNames);
        fallbackAssetNamePrefix = EditorGUILayout.TextField("Fallback Name Prefix", fallbackAssetNamePrefix);

        EditorGUILayout.Space();
        DrawMappingPreview();
        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!CanImport())) {
            if (GUILayout.Button("Import / Update Assets", GUILayout.Height(32f))) {
                ImportAssets();
            }
        }
    }

    private void DrawMappingPreview()
    {
        if (selectedTable == null) {
            EditorGUILayout.HelpBox("Choose a TsvTable asset to preview header-to-field mapping.", MessageType.Info);
            return;
        }

        Type targetType = scriptableObjectTypes[selectedTypeIndex];
        FieldInfo[] fields = GetImportableFields(targetType);
        HashSet<string> headers = new(selectedTable.Headers, StringComparer.OrdinalIgnoreCase);

        EditorGUILayout.LabelField("Mapped Fields", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150f));
        foreach (FieldInfo field in fields) {
            bool matched = headers.Contains(field.Name);
            string status = matched ? "Mapped" : "Missing";
            EditorGUILayout.LabelField($"{status}  {field.Name} ({field.FieldType.Name})");
        }
        EditorGUILayout.EndScrollView();
    }

    private void ImportAssets()
    {
        string outputFolderPath = AssetDatabase.GetAssetPath(outputFolder);
        if (string.IsNullOrWhiteSpace(outputFolderPath) || !AssetDatabase.IsValidFolder(outputFolderPath)) {
            EditorUtility.DisplayDialog("Invalid Folder", "Choose a valid output folder inside Assets.", "OK");
            return;
        }

        Type targetType = scriptableObjectTypes[selectedTypeIndex];
        FieldInfo[] fields = GetImportableFields(targetType);
        int importedCount = 0;
        int updatedCount = 0;

        AssetDatabase.StartAssetEditing();
        try {
            for (int rowIndex = 0; rowIndex < selectedTable.RowCount; rowIndex++) {
                string assetName = BuildAssetName(rowIndex);
                if (string.IsNullOrWhiteSpace(assetName)) {
                    assetName = $"{fallbackAssetNamePrefix}_{rowIndex + 1}";
                }

                string assetPath = $"{outputFolderPath}/{assetName}.asset";
                UnityEngine.Object existingAssetAtPath = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (existingAssetAtPath != null && !targetType.IsInstanceOfType(existingAssetAtPath)) {
                    Debug.LogWarning($"[TSV Importer] Skipped '{assetPath}' because a different asset type already exists there.");
                    continue;
                }

                ScriptableObject asset = AssetDatabase.LoadAssetAtPath(assetPath, targetType) as ScriptableObject;
                bool isNewAsset = asset == null;
                if (isNewAsset) {
                    asset = ScriptableObject.CreateInstance(targetType);
                    asset.name = assetName;
                }

                ApplyRowToAsset(asset, fields, rowIndex);

                if (isNewAsset) {
                    AssetDatabase.CreateAsset(asset, assetPath);
                    importedCount++;
                }
                else {
                    EditorUtility.SetDirty(asset);
                    updatedCount++;
                }
            }
        }
        finally {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "TSV Import Complete",
            $"Created {importedCount} asset(s) and updated {updatedCount} asset(s).",
            "OK");
    }

    private void ApplyRowToAsset(ScriptableObject asset, FieldInfo[] fields, int rowIndex)
    {
        foreach (FieldInfo field in fields) {
            if (!selectedTable.TryGetValue(rowIndex, field.Name, out string rawValue)) {
                continue;
            }

            if (!TryConvertValue(field.FieldType, rawValue, out object convertedValue)) {
                Debug.LogWarning($"[TSV Importer] Failed to convert '{rawValue}' to {field.FieldType.Name} for field '{field.Name}' on row {rowIndex + 2}.");
                continue;
            }

            field.SetValue(asset, convertedValue);
        }
    }

    private string BuildAssetName(int rowIndex)
    {
        if (selectedTable.Headers.Count == 0) {
            return $"{fallbackAssetNamePrefix}_{rowIndex + 1}";
        }

        string columnName = selectedTable.Headers[selectedNameColumnIndex];
        string assetName = selectedTable.GetValue(rowIndex, columnName, string.Empty).Trim();
        return SanitizeAssetName(assetName);
    }

    private static string SanitizeAssetName(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName)) {
            return string.Empty;
        }

        char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
        foreach (char invalidChar in invalidChars) {
            assetName = assetName.Replace(invalidChar, '_');
        }

        return assetName.Trim();
    }

    private string[] BuildColumnNames()
    {
        if (selectedTable == null || selectedTable.Headers.Count == 0) {
            return new[] { "(No Headers)" };
        }

        return selectedTable.Headers
            .Select((header, index) => string.IsNullOrWhiteSpace(header) ? $"Column {index + 1}" : header)
            .ToArray();
    }

    private bool CanImport()
    {
        return selectedTable != null
            && outputFolder != null
            && selectedTable.RowCount > 0
            && scriptableObjectTypes.Length > 0;
    }

    private void TryAssignSelection()
    {
        if (Selection.activeObject is TsvTable table) {
            selectedTable = table;
        }

        if (outputFolder == null) {
            outputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets");
        }
    }

    private void RefreshTypes()
    {
        scriptableObjectTypes = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
            .Where(type => !type.IsAbstract && !type.IsGenericType && type != typeof(TsvTable))
            .OrderBy(type => type.FullName)
            .ToArray();

        typeNames = scriptableObjectTypes
            .Select(type => type.FullName ?? type.Name)
            .ToArray();

        if (selectedTypeIndex >= scriptableObjectTypes.Length) {
            selectedTypeIndex = 0;
        }
    }

    private static FieldInfo[] GetImportableFields(Type targetType)
    {
        return targetType
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(field => !field.IsInitOnly)
            .ToArray();
    }

    private static bool TryConvertValue(Type targetType, string rawValue, out object convertedValue)
    {
        string trimmedValue = rawValue?.Trim() ?? string.Empty;

        if (targetType == typeof(string)) {
            convertedValue = rawValue ?? string.Empty;
            return true;
        }

        if (targetType == typeof(int)) {
            bool parsed = int.TryParse(trimmedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue);
            convertedValue = intValue;
            return parsed;
        }

        if (targetType == typeof(float)) {
            bool parsed = float.TryParse(trimmedValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float floatValue);
            convertedValue = floatValue;
            return parsed;
        }

        if (targetType == typeof(bool)) {
            if (bool.TryParse(trimmedValue, out bool boolValue)) {
                convertedValue = boolValue;
                return true;
            }

            if (trimmedValue == "0" || trimmedValue.Equals("no", StringComparison.OrdinalIgnoreCase)) {
                convertedValue = false;
                return true;
            }

            if (trimmedValue == "1" || trimmedValue.Equals("yes", StringComparison.OrdinalIgnoreCase)) {
                convertedValue = true;
                return true;
            }

            convertedValue = false;
            return false;
        }

        if (targetType.IsEnum) {
            try {
                convertedValue = Enum.Parse(targetType, trimmedValue, true);
                return true;
            }
            catch {
                convertedValue = Activator.CreateInstance(targetType);
                return false;
            }
        }

        if (targetType == typeof(Color)) {
            bool parsed = ColorUtility.TryParseHtmlString(trimmedValue, out Color colorValue)
                || TryParseColorComponents(trimmedValue, out colorValue);
            convertedValue = colorValue;
            return parsed;
        }

        if (targetType == typeof(Vector2)) {
            bool parsed = TryParseFloatArray(trimmedValue, 2, out float[] components);
            convertedValue = parsed ? new Vector2(components[0], components[1]) : default(Vector2);
            return parsed;
        }

        if (targetType == typeof(Vector3)) {
            bool parsed = TryParseFloatArray(trimmedValue, 3, out float[] components);
            convertedValue = parsed ? new Vector3(components[0], components[1], components[2]) : default(Vector3);
            return parsed;
        }

        convertedValue = null;
        return false;
    }

    private static bool TryParseColorComponents(string rawValue, out Color colorValue)
    {
        if (!TryParseFloatArray(rawValue, 3, out float[] rgbComponents) && !TryParseFloatArray(rawValue, 4, out rgbComponents)) {
            colorValue = default;
            return false;
        }

        float r = rgbComponents[0];
        float g = rgbComponents[1];
        float b = rgbComponents[2];
        float a = rgbComponents.Length > 3 ? rgbComponents[3] : 1f;
        colorValue = new Color(r, g, b, a);
        return true;
    }

    private static bool TryParseFloatArray(string rawValue, int requiredCount, out float[] components)
    {
        string[] parts = rawValue.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != requiredCount) {
            components = Array.Empty<float>();
            return false;
        }

        components = new float[requiredCount];
        for (int i = 0; i < requiredCount; i++) {
            if (!float.TryParse(parts[i].Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out components[i])) {
                components = Array.Empty<float>();
                return false;
            }
        }

        return true;
    }
}

[CustomEditor(typeof(TsvTable))]
public class TsvTableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        if (GUILayout.Button("Open TSV ScriptableObject Importer")) {
            TsvScriptableObjectImporterWindow.OpenWindow((TsvTable)target);
        }
    }
}
