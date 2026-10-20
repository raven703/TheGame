$root = 'c:\Personal\Unity\TheGame\My project'
$files = @(
 'Assets/Scripts/Ships/ShipDefinition.cs',
 'Assets/Scripts/Ships/ShipLibrary.cs',
 'Assets/Scripts/ShipRegistry.cs',
 'Assets/Scripts/PlayerProfile.cs'
)
foreach ($f in $files) {
    $meta = Join-Path $root (($f -replace '/','\') + '.meta')
    $g = (Select-String -Path $meta -Pattern 'guid: (\w+)').Matches[0].Groups[1].Value
    Write-Output ($g + '  ' + $f)
}
Write-Output '--- CobraMK3.asset ---'
Get-Content (Join-Path $root 'Assets/Ships/CobraMK3.asset')
Write-Output '--- ShipLibrary.asset ---'
Get-Content (Join-Path $root 'Assets/Ships/ShipLibrary.asset')
