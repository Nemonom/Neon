using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

public static class TsvGameDataImporters
{
    [MenuItem("Tools/TSV/Create GameDatas From Selected Table")]
    private static void CreateGameDatasFromSelectedTable()
    {
        if (Selection.activeObject is not TsvTable table) {
            EditorUtility.DisplayDialog("TSV Import", "Select a TsvTable asset first.", "OK");
            return;
        }

        string savePath = EditorUtility.SaveFilePanelInProject("Create GameDatas", table.name + "_GameDatas", "asset", "Choose where to save the GameDatas asset.");
        if (string.IsNullOrWhiteSpace(savePath)) {
            return;
        }

        Dictionary<string, List<string>> lookup = new();
        for (int rowIndex = 0; rowIndex < table.RowCount; rowIndex++) {
            string difficulty = table.GetValue(rowIndex, "difficulty");
            string word = table.GetValue(rowIndex, "word");
            if (string.IsNullOrWhiteSpace(difficulty) || string.IsNullOrWhiteSpace(word)) {
                continue;
            }

            if (!lookup.TryGetValue(difficulty.Trim(), out List<string> words)) {
                words = new List<string>();
                lookup.Add(difficulty.Trim(), words);
            }

            words.Add(word.Trim().ToUpperInvariant());
        }

        List<DifficultyWordBucket> buckets = new();
        foreach (KeyValuePair<string, List<string>> pair in lookup) {
            buckets.Add(new DifficultyWordBucket
            {
                difficultyKey = pair.Key,
                words = pair.Value
            });
        }

        GameDatas asset = ScriptableObject.CreateInstance<GameDatas>();
        asset.SetEntries(buckets);
        AssetDatabase.CreateAsset(asset, savePath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
    }

    [MenuItem("Tools/TSV/Create GamePlayDatas From Selected Table")]
    private static void CreateGamePlayDatasFromSelectedTable()
    {
        if (Selection.activeObject is not TsvTable table) {
            EditorUtility.DisplayDialog("TSV Import", "Select a TsvTable asset first.", "OK");
            return;
        }

        string savePath = EditorUtility.SaveFilePanelInProject("Create GamePlayDatas", table.name + "_GamePlayDatas", "asset", "Choose where to save the GamePlayDatas asset.");
        if (string.IsNullOrWhiteSpace(savePath)) {
            return;
        }

        List<GameplayDifficultyEntry> entries = new();
        for (int rowIndex = 0; rowIndex < table.RowCount; rowIndex++) {
            string difficulty = table.GetValue(rowIndex, "difficulty");
            if (string.IsNullOrWhiteSpace(difficulty)) {
                continue;
            }

            GameplayDifficultySettings settings = new()
            {
                spawnInterval = ParseFloat(table.GetValue(rowIndex, "spawnInterval"), 1.25f),
                maxEnemyCount = ParseInt(table.GetValue(rowIndex, "maxEnemyCount"), 8),
                enemySpeedMultiplier = ParseFloat(table.GetValue(rowIndex, "enemySpeedMultiplier"), 1f),
                contactDamage = ParseInt(table.GetValue(rowIndex, "contactDamage"), 12),
                scorePerLetter = ParseInt(table.GetValue(rowIndex, "scorePerLetter"), 10),
                comboBonusStep = ParseFloat(table.GetValue(rowIndex, "comboBonusStep"), 0.1f),
                startHp = ParseInt(table.GetValue(rowIndex, "startHp"), 100)
            };

            entries.Add(new GameplayDifficultyEntry
            {
                difficultyKey = difficulty.Trim(),
                settings = settings
            });
        }

        GamePlayDatas asset = ScriptableObject.CreateInstance<GamePlayDatas>();
        asset.SetEntries(entries);
        AssetDatabase.CreateAsset(asset, savePath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
    }

    private static float ParseFloat(string rawValue, float defaultValue)
    {
        return float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            ? value
            : defaultValue;
    }

    private static int ParseInt(string rawValue, int defaultValue)
    {
        return int.TryParse(rawValue, out int value) ? value : defaultValue;
    }
}
