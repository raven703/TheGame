$f='c:\Personal\Unity\TheGame\My project\SHIP_ARCHITECTURE_REPORT.md'
Select-String -Path $f -Pattern '^#{1,3} ' | ForEach-Object { $_.LineNumber.ToString() + ': ' + $_.Line }
