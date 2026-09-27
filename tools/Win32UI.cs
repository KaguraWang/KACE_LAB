// 窗口级 UI 自动化要用的 Win32 入口。
// 单独放一个 .cs 文件而不是用 PowerShell 的 here-string ——
// here-string 在带 BOM 的脚本里容易被解析器吃掉结尾的 "@。
using System;
using System.Runtime.InteropServices;

public class Win32UI {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint f, IntPtr e);
    [DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls, string name);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr h);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, System.Text.StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool f);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("shcore.dll")] public static extern int SetProcessDpiAwareness(int v);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    public const uint PW_RENDERFULLCONTENT = 2;

    public const int SW_RESTORE = 9;
    public const uint LEFTDOWN = 0x0002, LEFTUP = 0x0004, RIGHTDOWN = 0x0008, RIGHTUP = 0x0010;
    public const uint MIDDLEDOWN = 0x0020, MIDDLEUP = 0x0040, WHEEL = 0x0800;
    public const uint KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }

    // ---- SendInput：比 keybd_event 更可靠的键盘注入 ----
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion {
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public MOUSEINPUT mi;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint n, INPUT[] inputs, int size);

    public const uint INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_UNICODE = 0x0004;
    public const uint KEYEVENTF_SCANCODE = 0x0008;

    /// <summary>用 SendInput 发一次按键。useUnicode=true 时直接发字符（不依赖键盘布局）。</summary>
    public static bool SendKey(ushort vk, bool up, bool unicode = false, ushort scan = 0) {
        var inp = new INPUT[1];
        inp[0].type = INPUT_KEYBOARD;
        inp[0].U.ki.wVk = unicode ? (ushort)0 : vk;
        inp[0].U.ki.wScan = unicode ? vk : scan;
        inp[0].U.ki.dwFlags = (up ? KEYEVENTF_KEYUP : 0) | (unicode ? KEYEVENTF_UNICODE : 0);
        inp[0].U.ki.time = 0;
        inp[0].U.ki.dwExtraInfo = IntPtr.Zero;
        return SendInput(1, inp, Marshal.SizeOf(typeof(INPUT))) == 1;
    }

    /// <summary>用 SendInput 打一个字符（走 Unicode 路径，绕开键盘布局）。</summary>
    public static bool TypeChar(char c) {
        SendKey((ushort)c, false, true);
        SendKey((ushort)c, true, true);
        return true;
    }

    /// <summary>把当前线程的输入挂到目标窗口线程上，再抢前台 ——
    /// 单靠 SetForegroundWindow 常常静默失败，之后所有键盘注入都到不了程序。</summary>
    public static bool ForceForeground(IntPtr h) {
        IntPtr fg = GetForegroundWindow();
        if (fg == h) return true;
        uint target = GetWindowThreadProcessId(h, IntPtr.Zero);
        uint me = GetCurrentThreadId();
        bool ok = false;
        if (target != me && target != 0) {
            if (AttachThreadInput(me, target, true)) {
                ShowWindow(h, SW_RESTORE);
                ok = SetForegroundWindow(h);
                AttachThreadInput(me, target, false);
            }
        }
        if (!ok) { ShowWindow(h, SW_RESTORE); ok = SetForegroundWindow(h); }
        return ok;
    }

    public static string Title(IntPtr h) {
        int n = GetWindowTextLength(h);
        var sb = new System.Text.StringBuilder(n + 2);
        GetWindowText(h, sb, sb.Capacity);
        return sb.ToString();
    }

    public static bool MakeDpiAware() {
        try { SetProcessDpiAwareness(2); return true; } catch { }
        try { return SetProcessDPIAware(); } catch { }
        return false;
    }
}