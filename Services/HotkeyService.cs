using System;
using System.Windows.Interop;
using NexusIsland.Core;

namespace NexusIsland.Services;

public class HotkeyService : IDisposable
{
    private const int HOTKEY_ID = 9001;
    private IntPtr _hwnd;
    private HwndSource? _source;
    public event Action? HotkeyPressed;

    public void Register(IntPtr hwnd, uint modifiers = NativeMethods.MOD_WIN, uint vk = 0x20) // Default Win+Space
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(HwndHook);

        NativeMethods.RegisterHotKey(_hwnd, HOTKEY_ID, modifiers | NativeMethods.MOD_NOREPEAT, vk);
    }

    public void Unregister()
    {
        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_ID);
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        if (_source != null)
        {
            _source.RemoveHook(HwndHook);
            _source = null;
        }
    }
}
