param(
    [Parameter(Mandatory = $true)][int]$ProcId,
    [Parameter(Mandatory = $true)][int]$X,
    [Parameter(Mandatory = $true)][int]$Y
)
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WinClick {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
$p = Get-Process -Id $ProcId
$h = $p.MainWindowHandle
[WinClick]::ShowWindow($h, 5) | Out-Null
[WinClick]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 400
$r = New-Object WinClick+RECT
[WinClick]::GetWindowRect($h, [ref]$r) | Out-Null
$sx = $r.Left + $X
$sy = $r.Top + $Y
[WinClick]::SetCursorPos($sx, $sy) | Out-Null
Start-Sleep -Milliseconds 200
[WinClick]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 80
[WinClick]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
Write-Output ("clicked window({0},{1}) screen({2},{3}) rect=({4},{5})" -f $X, $Y, $sx, $sy, $r.Left, $r.Top)
