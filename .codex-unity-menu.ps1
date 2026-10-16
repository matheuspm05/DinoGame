param([string]$Section, [string]$Item)
Add-Type -TypeDefinition 'using System; using System.Text; using System.Runtime.InteropServices; public static class NaveMenu { [DllImport("user32.dll")] public static extern IntPtr GetMenu(IntPtr window); [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr menu); [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetMenuString(IntPtr menu, uint item, StringBuilder text, int count, uint flags); [DllImport("user32.dll")] public static extern IntPtr GetSubMenu(IntPtr menu, int index); [DllImport("user32.dll")] public static extern uint GetMenuItemID(IntPtr menu, int index); [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam); }'
$unityProcess = Get-Process Unity | Where-Object { $_.MainWindowTitle.StartsWith('My project - ') } | Select-Object -First 1
if (-not $unityProcess.MainWindowTitle.StartsWith('My project - ')) { throw 'Janela inesperada' }
$unityWindow = $unityProcess.MainWindowHandle

Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class OrganizationFocus { [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow(); [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr w,IntPtr p); [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId(); [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a,uint b,bool attach); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr w); [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr w); [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr w,int mode); }'
$organizationCurrent = [OrganizationFocus]::GetCurrentThreadId()
$organizationForeground = [OrganizationFocus]::GetWindowThreadProcessId([OrganizationFocus]::GetForegroundWindow(),[IntPtr]::Zero)
[void][OrganizationFocus]::AttachThreadInput($organizationCurrent,$organizationForeground,$true)
try { [void][OrganizationFocus]::ShowWindow($unityWindow,9); [void][OrganizationFocus]::BringWindowToTop($unityWindow); [void][OrganizationFocus]::SetForegroundWindow($unityWindow) } finally { [void][OrganizationFocus]::AttachThreadInput($organizationCurrent,$organizationForeground,$false) }

$unityMenu = [NaveMenu]::GetMenu($unityWindow)
for ($unityIndex = 0; $unityIndex -lt [NaveMenu]::GetMenuItemCount($unityMenu); $unityIndex++) {
 $unityTitle = New-Object System.Text.StringBuilder 256
 [void][NaveMenu]::GetMenuString($unityMenu, $unityIndex, $unityTitle, 256, 1024)
 if ($unityTitle.ToString().Replace('&','') -ne $Section) { continue }
 $unitySubmenu = [NaveMenu]::GetSubMenu($unityMenu, $unityIndex)
 for ($unityItem = 0; $unityItem -lt [NaveMenu]::GetMenuItemCount($unitySubmenu); $unityItem++) {
  $unityLabel = New-Object System.Text.StringBuilder 256
  [void][NaveMenu]::GetMenuString($unitySubmenu, $unityItem, $unityLabel, 256, 1024)
  $unityName = $unityLabel.ToString().Replace('&','').Split([char]9)[0]
  if (-not $Item) { Write-Output $unityName; continue }
  if ($unityName -eq $Item) {
   $unityCommand = [NaveMenu]::GetMenuItemID($unitySubmenu, $unityItem)
   Write-Output "Executando $Section > $unityName ($unityCommand)"
   [void][NaveMenu]::SendMessage($unityWindow, 273, [IntPtr]([long]$unityCommand), [IntPtr]0)
   exit 0
  }
 }
}
if ($Item) { throw "Menu nao encontrado: $Section > $Item" }
