# csharptest

WPF test harness for `../dwgcore`'s native interop DLL. See README.md for
what it is and how to run it. This file is conventions/gotchas only.

## Keep `NativeInterop.cs` in sync with `dwgcore_api.h`, not the other way around

`../dwgcore/src/dwgcore_api.h` is the authoritative contract. If you add or
change an exported function there, mirror it here -- including the
`CharSet` on any function taking a `path`/string parameter:
`DwgCore_LoadFile`'s `path` is the OS active code page (`CharSet.Ansi`),
**not** UTF-8, because `libdxfrw` opens it as a plain narrow `const char*`
(see the comment on `DwgCore_LoadFile` in `dwgcore_api.h` for why). Getting
this marshaling attribute wrong won't necessarily throw -- it'll just mangle
non-ASCII paths silently.

## Why `DwgHwndHost.ArrangeOverride` calls `MoveWindow` itself

`HwndHost` resizes its own intermediate host window to fit WPF layout, but
Win32 doesn't propagate that to a reparented child window automatically --
nothing here does that for you. `ArrangeOverride`'s explicit `MoveWindow`
call is what actually delivers a real `WM_SIZE` to Qt's window proc; without
it the embedded viewport would stay whatever size it was created at
regardless of how the WPF window is resized.

## Why there's a 32-bit code path in `DwgHwndHost`'s Win32 declarations

`GetWindowLongPtr`/`SetWindowLongPtr` don't exist in a meaningful form in
32-bit `user32.dll` -- only the plain (pointer-truncating) `GetWindowLong`/
`SetWindowLong` do. Since this project builds both `x64` and `x86` (see
`DwgCoreTest.csproj`'s `Platforms`), both entry points are declared and
`IntPtr.Size == 8` picks the right one at runtime. Don't delete the 32-bit
branch as "dead code" -- it's exercised by the `x86` build configuration.

## If the window doesn't appear after changing `dwgcore.dll`

Rebuild *and* rerun `dotnet build` for this project before assuming
anything is broken -- the `.csproj`'s `QtRuntimeFiles` item only copies
whatever is currently sitting in `../dwgcore/build{,-x86}/Release`; a stale
copy left over from before a native-side change is a much more likely
explanation than a real regression. See README.md's note on the
first-launch-after-rebuild symptom, and
`../dwgcore/CLAUDE.md`'s `interop_smoke_test.cpp` for isolating the native
DLL from this WPF layer entirely when something looks wrong.
