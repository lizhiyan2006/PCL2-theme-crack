$ErrorActionPreference = 'SilentlyContinue'
$dir = 'C:\Users\lizhiyan394\Documents\deepseek-harness\default-workspace\pcl-work\test'
$src = 'C:\Users\lizhiyan394\Documents\deepseek-harness\default-workspace\pcl-work\tools\PCLX\bin\Debug\net48\PCLX.dll'

Get-Process 'Plain Craft Launcher 2' | Where-Object { $_.Path -like '*pcl-work*' } | Stop-Process -Force
Start-Sleep -Seconds 3
Copy-Item $src ($dir + '\PCLX.dll') -Force
Remove-Item ($dir + '\PCL\Log*.txt'), ($dir + '\PCLX.log') -Force
Start-Sleep -Milliseconds 300
Start-Process explorer.exe -ArgumentList ($dir + '\start-test.bat')
Start-Sleep -Seconds 16
Get-Process 'Plain Craft Launcher 2' | Where-Object { $_.Path -like '*pcl-work*' } |
    Select-Object Id, MainWindowTitle | Format-Table -AutoSize
