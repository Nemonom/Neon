[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$ExcelPath,

    [Parameter(Position = 1)]
    [string]$OutputDirectory,

    [string[]]$SheetNames
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-AbsolutePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction Stop
    return $resolved.Path
}

function Get-SafeFileName {
    param([Parameter(Mandatory = $true)][string]$Name)

    $invalidChars = [System.IO.Path]::GetInvalidFileNameChars()
    $safeName = $Name
    foreach ($char in $invalidChars) {
        $safeName = $safeName.Replace($char, "_")
    }

    return $safeName
}

function Convert-CellToTsvField {
    param($Value)

    if ($null -eq $Value) {
        return ""
    }

    $text = [string]$Value
    $needsQuotes = $text.Contains("`t") -or $text.Contains("`n") -or $text.Contains("`r") -or $text.Contains('"')
    if (-not $needsQuotes) {
        return $text
    }

    return '"' + $text.Replace('"', '""') + '"'
}

$absoluteExcelPath = Resolve-AbsolutePath -Path $ExcelPath

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $workbookName = [System.IO.Path]::GetFileNameWithoutExtension($absoluteExcelPath)
    $OutputDirectory = Join-Path -Path (Split-Path -Parent $absoluteExcelPath) -ChildPath ($workbookName + "_tsv")
}

$absoluteOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $absoluteOutputDirectory | Out-Null

$excel = $null
$workbook = $null

try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false

    $workbook = $excel.Workbooks.Open($absoluteExcelPath)
    $selectedSheets = @()

    if ($SheetNames -and $SheetNames.Count -gt 0) {
        foreach ($sheetName in $SheetNames) {
            $matchedSheet = $null
            foreach ($worksheet in $workbook.Worksheets) {
                if ($worksheet.Name -eq $sheetName) {
                    $matchedSheet = $worksheet
                    break
                }
            }

            if ($null -eq $matchedSheet) {
                throw "Worksheet '$sheetName' was not found in '$absoluteExcelPath'."
            }

            $selectedSheets += $matchedSheet
        }
    }
    else {
        foreach ($worksheet in $workbook.Worksheets) {
            $selectedSheets += $worksheet
        }
    }

    foreach ($worksheet in $selectedSheets) {
        $usedRange = $worksheet.UsedRange
        $rowCount = [int]$usedRange.Rows.Count
        $columnCount = [int]$usedRange.Columns.Count
        $sheetFileName = Get-SafeFileName -Name $worksheet.Name
        $outputPath = Join-Path -Path $absoluteOutputDirectory -ChildPath ($sheetFileName + ".tsv")
        $lines = New-Object System.Collections.Generic.List[string]

        for ($row = 1; $row -le $rowCount; $row++) {
            $fields = New-Object string[] $columnCount

            for ($column = 1; $column -le $columnCount; $column++) {
                $cellText = $usedRange.Cells.Item($row, $column).Text
                $fields[$column - 1] = Convert-CellToTsvField -Value $cellText
            }

            $lines.Add(($fields -join "`t"))
        }

        [System.IO.File]::WriteAllLines($outputPath, $lines, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Exported '$($worksheet.Name)' -> $outputPath"
    }
}
finally {
    if ($null -ne $workbook) {
        $workbook.Close($false)
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($workbook)
    }

    if ($null -ne $excel) {
        $excel.Quit()
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel)
    }

    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
