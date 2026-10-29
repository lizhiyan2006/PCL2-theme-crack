param([string]$Name = "设置…")
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$p = Get-Process 'Plain Craft Launcher 2' | Where-Object { $_.Path -like '*pcl-work*' } | Select-Object -First 1
if (-not $p) { Write-Output "no process"; exit 1 }
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Name)
$el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
if (-not $el) { Write-Output "not found: $Name"; exit 2 }
$pat = $null
if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pat)) {
    $pat.Invoke()
    Write-Output ("invoked " + $Name)
} else {
    $r = $el.Current.BoundingRectangle
    Write-Output ("no invoke pattern; rect=" + $r)
}
