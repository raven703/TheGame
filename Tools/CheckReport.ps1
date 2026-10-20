$report = 'c:\Personal\Unity\TheGame\My project\SHIP_ARCHITECTURE_REPORT.md'
$patterns = @('Назначение кнопок', 'public class TacticalCommandPanel', 'TacticalCommandPanel.cs', 'EnemyShip.prefab', 'FittingWindow.prefab', 'CobraMK3.asset')
foreach ($p in $patterns) {
    $m = Select-String -Path $report -Pattern $p
    Write-Output ("--- " + $p + " : " + $m.Count)
    foreach ($x in $m) { Write-Output ("   " + $x.LineNumber + ": " + $x.Line.Trim()) }
}
