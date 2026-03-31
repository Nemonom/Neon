using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TsvTable", menuName = "Scriptable Objects/TsvTable")]
public class TsvTable : ScriptableObject
{
    [SerializeField] private List<string> headers = new();
    [SerializeField] private List<TsvRow> rows = new();

    private Dictionary<string, int> headerLookup;

    public IReadOnlyList<string> Headers => headers;
    public IReadOnlyList<TsvRow> Rows => rows;
    public int RowCount => rows.Count;
    public int ColumnCount => headers.Count;

    public void SetData(List<string> newHeaders, List<TsvRow> newRows)
    {
        headers = newHeaders ?? new List<string>();
        rows = newRows ?? new List<TsvRow>();
        headerLookup = null;
    }

    public int GetColumnIndex(string columnName)
    {
        if (string.IsNullOrEmpty(columnName)) {
            return -1;
        }

        EnsureHeaderLookup();
        return headerLookup.TryGetValue(columnName, out int index) ? index : -1;
    }

    public bool TryGetValue(int rowIndex, string columnName, out string value)
    {
        value = string.Empty;

        int columnIndex = GetColumnIndex(columnName);
        if (columnIndex < 0) {
            return false;
        }

        if (rowIndex < 0 || rowIndex >= rows.Count) {
            return false;
        }

        return rows[rowIndex].TryGetValue(columnIndex, out value);
    }

    public string GetValue(int rowIndex, string columnName, string defaultValue = "")
    {
        return TryGetValue(rowIndex, columnName, out string value) ? value : defaultValue;
    }

    public Dictionary<string, string> GetRowDictionary(int rowIndex)
    {
        Dictionary<string, string> rowData = new(headers.Count);
        if (rowIndex < 0 || rowIndex >= rows.Count) {
            return rowData;
        }

        TsvRow row = rows[rowIndex];
        for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++) {
            rowData[headers[columnIndex]] = row.GetValueOrDefault(columnIndex);
        }

        return rowData;
    }

    private void OnEnable()
    {
        headerLookup = null;
    }

    private void EnsureHeaderLookup()
    {
        if (headerLookup != null) {
            return;
        }

        headerLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++) {
            string header = headers[i];
            if (string.IsNullOrWhiteSpace(header) || headerLookup.ContainsKey(header)) {
                continue;
            }

            headerLookup.Add(header, i);
        }
    }
}

[Serializable]
public class TsvRow
{
    [SerializeField] private List<string> values = new();

    public IReadOnlyList<string> Values => values;

    public TsvRow()
    {
    }

    public TsvRow(List<string> sourceValues)
    {
        values = sourceValues ?? new List<string>();
    }

    public bool TryGetValue(int columnIndex, out string value)
    {
        value = string.Empty;
        if (columnIndex < 0 || columnIndex >= values.Count) {
            return false;
        }

        value = values[columnIndex];
        return true;
    }

    public string GetValueOrDefault(int columnIndex, string defaultValue = "")
    {
        return TryGetValue(columnIndex, out string value) ? value : defaultValue;
    }
}
