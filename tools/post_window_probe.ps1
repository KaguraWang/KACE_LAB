param([Parameter(Mandatory=$true)][int]$TargetProcessId)

# Posts to one ChemLabUnity HWND. Never activates the window or moves the OS cursor.
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public static class ChemLabWindowMessages {
    delegate bool Enumerator(IntPtr hwnd, IntPtr ignored);
    [DllImport("user32.dll")] static extern bool EnumWindows(Enumerator callback, IntPtr ignored);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int capacity);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam);
    public static IntPtr[] ForProcess(int processId) {
        var found=new List<IntPtr>();
        EnumWindows((hwnd, ignored) => {
            uint owner; GetWindowThreadProcessId(hwnd,out owner);
            if(owner==(uint)processId){
                var text=new StringBuilder(512);GetWindowText(hwnd,text,text.Capacity);
                if(text.ToString()=="ChemLabUnity")found.Add(hwnd);
            }
            return true;
        },IntPtr.Zero);
        return found.ToArray();
    }
    public static bool Send(IntPtr hwnd,uint message,int wparam,int lparam) {
        return PostMessage(hwnd,message,new IntPtr(wparam),new IntPtr(lparam));
    }
    public static int Point(int x,int y) { return (y<<16)|(x&0xffff); }
}
'@
$windows=[ChemLabWindowMessages]::ForProcess($TargetProcessId)
if($windows.Length -ne 1){throw "Expected exactly one ChemLabUnity window for PID $TargetProcessId; found $($windows.Length)"}
$target=$windows[0]
$point=[ChemLabWindowMessages]::Point(320,240)
if(-not [ChemLabWindowMessages]::Send($target,0x200,0,$point)){throw 'WM_MOUSEMOVE failed'}
Start-Sleep -Milliseconds 250
if(-not [ChemLabWindowMessages]::Send($target,0x201,1,$point)){throw 'WM_LBUTTONDOWN failed'}
Start-Sleep -Milliseconds 250
if(-not [ChemLabWindowMessages]::Send($target,0x202,0,$point)){throw 'WM_LBUTTONUP failed'}
Start-Sleep -Milliseconds 250
if(-not [ChemLabWindowMessages]::Send($target,0x100,0x57,1)){throw 'WM_KEYDOWN W failed'}
Start-Sleep -Milliseconds 250
if(-not [ChemLabWindowMessages]::Send($target,0x101,0x57,-1073741823)){throw 'WM_KEYUP W failed'}
"POSTED hwnd=$target pid=$TargetProcessId"
