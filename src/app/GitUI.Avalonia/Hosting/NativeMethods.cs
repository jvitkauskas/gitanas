using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Win32 interop used to host Avalonia windows over native owner windows on Windows (other systems use the owners of
///  Avalonia, docs/avalonia-port/CROSS-PLATFORM.md, phase 2).
/// </summary>
[SupportedOSPlatform("windows")]
internal static class NativeMethods
{
    public const uint GA_ROOT = 2;
    public const uint GW_OWNER = 4;
    public const int GWLP_HWNDPARENT = -8;
    public const uint WM_WINDOWPOSCHANGING = 0x0046;
    public const uint WM_NCHITTEST = 0x0084;
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint WM_NCACTIVATE = 0x0086;
    public const nint HTLEFT = 10;
    public const nint HTRIGHT = 11;
    public const nint HTTOP = 12;
    public const nint HTTOPLEFT = 13;
    public const nint HTTOPRIGHT = 14;
    public const nint HTBOTTOM = 15;
    public const nint HTBOTTOMLEFT = 16;
    public const nint HTBOTTOMRIGHT = 17;
    public const nint HTBORDER = 18;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPOS
    {
        public nint Hwnd;
        public nint HwndInsertAfter;
        public int X;
        public int Y;
        public int Cx;
        public int Cy;
        public uint Flags;
    }

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(nint hWnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    public static extern nint SendMessage(nint hWnd, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern nint GetAncestor(nint hwnd, uint gaFlags);

    [DllImport("user32.dll")]
    public static extern nint GetWindow(nint hWnd, uint uCmd);

    [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
    public static extern nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    public static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpfn, nint lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    /// <summary>
    ///  Disables the visible, enabled top-level windows of the current thread (as WinForms does for modal forms).
    /// </summary>
    public static List<nint> DisableEnabledThreadWindows(nint except)
    {
        List<nint> windows = [];
        EnumThreadWindows(
            GetCurrentThreadId(),
            (hWnd, _) =>
            {
                if (hWnd != except && IsWindowVisible(hWnd) && IsWindowEnabled(hWnd))
                {
                    windows.Add(hWnd);
                }

                return true;
            },
            0);

        foreach (nint window in windows)
        {
            EnableWindow(window, false);
        }

        return windows;
    }

    public static void EnableWindows(IEnumerable<nint> windows)
    {
        foreach (nint window in windows)
        {
            EnableWindow(window, true);
        }
    }
}
