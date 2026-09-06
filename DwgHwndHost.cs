using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DwgCoreTest;

// Reparents one dwgcore viewport's native HWND into the WPF visual tree.
// See ../dwgcore/README.md's "Using it from C#" section -- this class is
// that sequence, implemented.
public sealed class DwgHwndHost : HwndHost
{
    private const int GWL_STYLE = -16;
    private const long WS_CHILD = 0x40000000;
    private const long WS_POPUP = unchecked((long)0x80000000);
    private const long WS_CAPTION = 0x00C00000;
    private const long WS_VISIBLE = 0x10000000;

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

    // GetWindowLongPtr/SetWindowLongPtr don't exist as such in 32-bit
    // user32.dll -- only the plain (32-bit-only) GetWindowLong/
    // SetWindowLong do. This project builds both x86 and x64 (see
    // DwgCoreTest.csproj), so both paths are needed.
    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) =>
        IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : (IntPtr)GetWindowLong32(hWnd, nIndex);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) =>
        IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : (IntPtr)SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32());

    // The dwgcore session handle (opaque, from DwgCore_CreateView) -- pass
    // this to DwgCore_LoadFile/ZoomFit/etc, not the raw HWND.
    public IntPtr ViewHandle { get; private set; } = IntPtr.Zero;

    private IntPtr childHwnd = IntPtr.Zero;

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        ViewHandle = NativeMethods.DwgCore_CreateView();
        if (ViewHandle == IntPtr.Zero)
            throw new InvalidOperationException("DwgCore_CreateView failed.");

        childHwnd = NativeMethods.DwgCore_GetNativeHandle(ViewHandle);
        if (childHwnd == IntPtr.Zero)
            throw new InvalidOperationException("DwgCore_GetNativeHandle failed.");

        // Swap the Qt top-level window's styles for a child window's
        // before reparenting -- WS_POPUP/WS_CAPTION would otherwise fight
        // WPF's layout (keep their own border/title bar, not follow being
        // resized as a child).
        long style = (long)GetWindowLongPtr(childHwnd, GWL_STYLE);
        style &= ~(WS_POPUP | WS_CAPTION);
        style |= WS_CHILD | WS_VISIBLE;
        SetWindowLongPtr(childHwnd, GWL_STYLE, (IntPtr)style);

        SetParent(childHwnd, hwndParent.Handle);

        // Only now that it's reparented -- showing it earlier would flash
        // a real top-level/taskbar window (see dwgcore_api.h).
        NativeMethods.DwgCore_SetVisible(ViewHandle, true);

        return new HandleRef(this, childHwnd);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        if (ViewHandle != IntPtr.Zero)
        {
            NativeMethods.DwgCore_DestroyView(ViewHandle);
            ViewHandle = IntPtr.Zero;
        }
        childHwnd = IntPtr.Zero;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // HwndHost resizes its own intermediate host window to fit WPF
        // layout, but Win32 doesn't propagate that to a reparented child on
        // its own -- explicitly resize the embedded HWND to match. This is
        // what actually delivers WM_SIZE to Qt's window proc (see
        // dwgcore_api.h's DwgCore_ResizeView comment on why that call is
        // usually redundant with this).
        if (childHwnd != IntPtr.Zero)
        {
            MoveWindow(childHwnd, 0, 0, (int)finalSize.Width, (int)finalSize.Height, true);
        }
        return finalSize;
    }
}
