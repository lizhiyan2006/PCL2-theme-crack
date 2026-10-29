$ErrorActionPreference = 'SilentlyContinue'
$dir = 'C:\Users\lizhiyan394\Documents\deepseek-harness\default-workspace\pcl-work'
$test = $dir + '\test'

Get-Process 'Plain Craft Launcher 2' | Where-Object { $_.Path -like '*pcl-work*' } | Stop-Process -Force
Start-Sleep -Seconds 3
Copy-Item ($dir + '\tools\PCLX\bin\Debug\net48\PCLX.dll') ($test + '\PCLX.dll') -Force
Remove-Item ($test + '\PCL\Log*.txt'), ($test + '\PCLX.log') -Force
Start-Sleep -Milliseconds 300
Start-Process explorer.exe -ArgumentList ($test + '\start-test.bat')

function Capture([string]$name) {
    $p = Get-Process 'Plain Craft Launcher 2' | Where-Object { $_.Path -like '*pcl-work*' } | Select-Object -First 1
    if ($null -eq $p) { return }
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File ($dir + '\tools\shot.ps1') -ProcId $p.Id -Out ($dir + '\' + $name)
}

Start-Sleep -Seconds 12
Capture 'v_theme.png'
Start-Sleep -Seconds 7
Capture 'v_system.png'
Start-Sleep -Seconds 7
Capture 'v_about.png'
Get-Content ($test + '\PCLX.log') -Encoding UTF8
