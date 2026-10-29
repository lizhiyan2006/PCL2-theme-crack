param([int]$ProcId, [string]$Filter = "")
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$p = Get-Process -Id $ProcId
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
$cond = [System.Windows.Automation.Condition]::TrueCondition
$all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
Write-Output ("count=" + $all.Count)
foreach ($e in $all) {
    $n = $e.Current.Name
    $t = $e.Current.ControlType.ProgrammaticName
    if ($Filter -eq "" -or $n -like $Filter) {
        Write-Output ("{0} | {1} | {2}" -f $t, $n, $e.Current.AutomationId)
    }
}
