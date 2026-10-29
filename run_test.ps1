$ErrorActionPreference = 'SilentlyContinue'
$dir = 'C:\Users\lizhiyan394\Documents\deepseek-harness\default-workspace\pcl-work\test'

Get-Process 'Plain Craft Launcher 2' | Where-Object { $_.Path -like '*pcl-work*' } | Stop-Process -Force
Start-Sleep -Seconds 3

Set-ItemProperty 'HKCU:\Software\PCL' -Name 'LaunchFolders' -Value ('mc>' + $dir + '\mc\.minecraft\')
Remove-Item ($dir + '\PCL\Log1.txt'), ($dir + '\PCLX.log') -Force
Start-Sleep -Milliseconds 300

Start-Process explorer.exe -ArgumentList ($dir + '\start-test.bat')
Start-Sleep -Seconds 18

Get-Process 'Plain Craft Launcher 2' | Where-Object { $_.Path -like '*pcl-work*' } |
    Select-Object Id, MainWindowTitle | Format-Table -AutoSize
