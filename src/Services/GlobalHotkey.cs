using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Xhair.Services;

public sealed class GlobalHotkey : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeydown = 0x0100;

    private readonly Keys _key;
    private readonly Action _onPressed;
    private readonly LowLevelKeyboardProc _proc;
    private readonly IntPtr _hookId;

    public GlobalHotkey(Keys key, Action onPressed)
    {
        _key = key;
        _onPressed = onPressed;
        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(_hookId);
        GC.SuppressFinalize(this);
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using Process curProcess = Process.GetCurrentProcess();
        using ProcessModule? curModule = curProcess.MainModule;
        if (curModule == null)
        {
            return IntPtr.Zero;
        }

        return SetWindowsHookEx(WhKeyboardLl, proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WmKeydown)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if ((Keys)vkCode == _key)
            {
                _onPressed();
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
