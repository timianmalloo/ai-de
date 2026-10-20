# Read-only: samples the OS left-button state. Injects no input.
Add-Type -Namespace W -Name U -MemberDefinition '[DllImport("user32.dll")] public static extern short GetAsyncKeyState(int k); [DllImport("user32.dll")] public static extern int GetSystemMetrics(int i);'
$v = @()
for ($i = 0; $i -lt 10; $i++) { $v += ('0x{0:X4}' -f ([int][W.U]::GetAsyncKeyState(1) -band 0xFFFF)); Start-Sleep -Milliseconds 300 }
"{0} GetAsyncKeyState(0x01) x10: {1} ; SM_SWAPBUTTON={2}" -f (Get-Date).ToUniversalTime().ToString('HH:mm:ssZ'), ($v -join ' '), [W.U]::GetSystemMetrics(23)
