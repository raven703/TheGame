$ErrorActionPreference = 'Stop'
$root = 'c:\Personal\Unity\TheGame\My project'
$partsDir = Join-Path $root 'Tools\parts'
$report = Join-Path $root 'SHIP_ARCHITECTURE_REPORT.md'
$fence = ([string][char]96) * 3

$parts = Get-ChildItem -Path $partsDir -Filter 'p*.md' | Sort-Object Name
$text = ''
foreach ($p in $parts) {
    $t = [System.IO.File]::ReadAllText($p.FullName) -replace "`r`n", "`n"
    $text += $t.TrimEnd() + "`n`n"
}

$missing = @()
$text = [regex]::Replace($text, '(?ms)@@CODE:(.+?)@@', {
    param($m)
    $rel = $m.Groups[1].Value.Trim()
    $path = Join-Path $root ($rel -replace '/', '\')
    if (Test-Path $path) {
        $code = ([System.IO.File]::ReadAllText($path)) -replace "`r`n", "`n"
        return ($fence + "csharp`n" + $code.TrimEnd() + "`n" + $fence)
    } else {
        $script:missing += $rel
        return ($fence + "csharp`n// FILE NOT FOUND: $rel`n" + $fence)
    }
})

[System.IO.File]::WriteAllText($report, $text)
Write-Output ("Parts: " + ($parts.Name -join ', '))
Write-Output ("Missing files: " + ($missing -join ', '))
Write-Output ("Length: " + $text.Length)
Write-Output ("Code blocks: " + ([regex]::Matches($text, [regex]::Escape($fence + 'csharp'))).Count)
Write-Output ("Fence lines: " + ([regex]::Matches($text, '(?m)^```')).Count)
Write-Output ("Leftover placeholders: " + ([regex]::Matches($text, '@@CODE:')).Count)
