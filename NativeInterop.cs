using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DwgCoreTest;

// P/Invoke declarations for dwgcore.dll. Mirrors
// ../dwgcore/src/dwgcore_api.h exactly -- that header is the authoritative
// contract (including the threading rule behind it); keep this in sync
// with it, not the other way around.
internal static class NativeMethods
{
    private const string DllName = "dwgcore.dll";

    [DllImport(DllName)]
    public static extern IntPtr DwgCore_CreateView();

    [DllImport(DllName)]
    public static extern IntPtr DwgCore_GetNativeHandle(IntPtr handle);

    // `path` is active-code-page (CharSet.Ansi), not UTF-8 -- see
    // dwgcore_api.h's DwgCore_LoadFile comment for why.
    [DllImport(DllName, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool DwgCore_LoadFile(IntPtr handle, string path);

    [DllImport(DllName, CharSet = CharSet.Ansi)]
    public static extern void DwgCore_GetLastError(IntPtr handle, StringBuilder buffer, int bufferLen);

    [DllImport(DllName)]
    public static extern void DwgCore_SetVisible(IntPtr handle, [MarshalAs(UnmanagedType.I1)] bool visible);

    [DllImport(DllName)]
    public static extern void DwgCore_ResizeView(IntPtr handle, int width, int height);

    [DllImport(DllName)]
    public static extern void DwgCore_ZoomFit(IntPtr handle);

    [DllImport(DllName)]
    public static extern void DwgCore_DestroyView(IntPtr handle);

    [DllImport(DllName)]
    public static extern void DwgCore_Shutdown();

    public static string GetLastError(IntPtr handle)
    {
        var buffer = new StringBuilder(1024);
        DwgCore_GetLastError(handle, buffer, buffer.Capacity);
        return buffer.ToString();
    }
}
