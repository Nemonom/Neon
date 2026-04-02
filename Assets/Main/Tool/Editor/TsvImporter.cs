using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, "tsv")]
public class TsvImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        string absolutePath = Path.GetFullPath(ctx.assetPath);
        string text = File.ReadAllText(absolutePath, Encoding.UTF8);
        List<List<string>> parsedRows = TsvParser.Parse(text);

        TsvTable table = ScriptableObject.CreateInstance<TsvTable>();
        table.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
        table.SetData(BuildHeaders(parsedRows), BuildRows(parsedRows));

        ctx.AddObjectToAsset("TsvTable", table);
        ctx.SetMainObject(table);
    }

    private static List<string> BuildHeaders(List<List<string>> parsedRows)
    {
        if (parsedRows.Count == 0) {
            return new List<string>();
        }

        return new List<string>(parsedRows[0]);
    }

    private static List<TsvRow> BuildRows(List<List<string>> parsedRows)
    {
        List<TsvRow> rows = new();
        if (parsedRows.Count <= 1) {
            return rows;
        }

        int columnCount = parsedRows[0].Count;
        for (int rowIndex = 1; rowIndex < parsedRows.Count; rowIndex++) {
            List<string> normalizedRow = NormalizeRow(parsedRows[rowIndex], columnCount);
            if (IsCompletelyEmpty(normalizedRow)) {
                continue;
            }

            rows.Add(new TsvRow(normalizedRow));
        }

        return rows;
    }

    private static List<string> NormalizeRow(List<string> row, int columnCount)
    {
        List<string> normalized = new(columnCount);
        for (int columnIndex = 0; columnIndex < columnCount; columnIndex++) {
            normalized.Add(columnIndex < row.Count ? row[columnIndex] : string.Empty);
        }

        return normalized;
    }

    private static bool IsCompletelyEmpty(List<string> row)
    {
        for (int i = 0; i < row.Count; i++) {
            if (!string.IsNullOrWhiteSpace(row[i])) {
                return false;
            }
        }

        return true;
    }
}

internal static class TsvParser
{
    public static List<List<string>> Parse(string source)
    {
        List<List<string>> rows = new();
        if (string.IsNullOrEmpty(source)) {
            return rows;
        }

        List<string> currentRow = new();
        StringBuilder currentCell = new();
        bool insideQuotes = false;

        for (int i = 0; i < source.Length; i++) {
            char current = source[i];

            if (insideQuotes) {
                if (current == '"') {
                    bool escapedQuote = i + 1 < source.Length && source[i + 1] == '"';
                    if (escapedQuote) {
                        currentCell.Append('"');
                        i++;
                    }
                    else {
                        insideQuotes = false;
                    }
                }
                else {
                    currentCell.Append(current);
                }

                continue;
            }

            if (current == '"') {
                insideQuotes = true;
                continue;
            }

            if (current == '\t') {
                currentRow.Add(currentCell.ToString());
                currentCell.Clear();
                continue;
            }

            if (current == '\r') {
                continue;
            }

            if (current == '\n') {
                currentRow.Add(currentCell.ToString());
                rows.Add(currentRow);
                currentRow = new List<string>();
                currentCell.Clear();
                continue;
            }

            currentCell.Append(current);
        }

        bool hasTrailingData = currentCell.Length > 0 || currentRow.Count > 0;
        if (hasTrailingData) {
            currentRow.Add(currentCell.ToString());
            rows.Add(currentRow);
        }

        return rows;
    }
}
