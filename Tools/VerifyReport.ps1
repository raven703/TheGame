$root = 'c:\Personal\Unity\TheGame\My project'
$report = Join-Path $root 'SHIP_ARCHITECTURE_REPORT.md'
$lines = [System.IO.File]::ReadAllLines($report)
$fence = ([string][char]96) * 3

# карта: содержимое файла -> путь
$sources = @{}
Get-ChildItem -Path (Join-Path $root 'Assets\Scripts') -Recurse -Filter *.cs | ForEach-Object {
    $c = ([System.IO.File]::ReadAllText($_.FullName)) -replace "`r`n","`n"
    $sources[$c.TrimEnd()] = $_.FullName.Substring($root.Length + 1)
}

$inBlock = $false
$lang = $null
$buf = New-Object System.Collections.Generic.List[string]
$ok = 0; $bad = 0; $skipped = 0; $notFound = 0

for ($i = 0; $i -lt $lines.Length; $i++) {
    $l = $lines[$i]
    if (-not $inBlock -and $l.StartsWith($fence) -and $l -ne $fence) {
        $inBlock = $true; $lang = $l.Substring(3); $buf.Clear(); continue
    }
    if ($inBlock -and $l -eq $fence) {
        $inBlock = $false
        if ($lang -ne 'csharp') { $skipped++; continue }
        $content = ($buf -join "`n").Trim()
        if ($sources.ContainsKey($content)) { Write-Output ("OK   " + $sources[$content] + " (" + $buf.Count + " lines)"); $ok++ }
        else { Write-Output ("NOT-FOUND block at line " + ($i+1) + " first=" + $buf[0]); $notFound++ }
        continue
    }
    if ($inBlock) { $buf.Add($l) }
}
Write-Output "OK=$ok NOT_FOUND=$notFound SKIPPED(non-csharp)=$skipped"
Write-Output ("Unclosed block: " + $inBlock)
$fences = 0
foreach ($l in $lines) { if ($l.StartsWith($fence)) { $fences++ } }
Write-Output ("Fence count: " + $fences + " (even=" + ($fences % 2 -eq 0) + ")")
Write-Output ("Total lines: " + $lines.Length)
