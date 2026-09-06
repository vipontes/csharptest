# csharptest

A minimal WPF application that exists to verify `../dwgcore`'s native
interop DLL end to end: HWND embedding via `HwndHost`, and the cross-thread
marshaling in `dwgcore`'s `QtEngine`. It is a test harness, not a sample of
how to build a real application against `dwgcore.dll` -- though the pattern
it uses (`NativeInterop.cs` + `DwgHwndHost.cs`) is exactly what a real
consumer should copy.

See `../dwgcore/README.md` for the full design this app is exercising.

## Files

- `NativeInterop.cs` -- P/Invoke declarations, mirroring
  `../dwgcore/src/dwgcore_api.h` exactly.
- `DwgHwndHost.cs` -- the `HwndHost` subclass that does the actual
  `SetParent` + style-bit surgery to reparent a `dwgcore` viewport's native
  HWND into the WPF visual tree.
- `MainWindow.xaml`/`.xaml.cs` -- hosts one `DwgHwndHost`, an Open-file
  button, and a Zoom Fit button. Also accepts an optional
  `DwgCoreTest.exe path\to\file.dxf` command-line argument (mirroring
  `dwgviewer`'s own `argv[1]` convenience) that auto-loads a file on
  startup -- useful for driving this harness without needing to automate
  the Open file dialog.
- `App.xaml.cs` -- calls `DwgCore_Shutdown()` on exit. This is mandatory,
  not optional cleanup (see `dwgcore_api.h`): there is no `DllMain`-based
  teardown in `dwgcore.dll`.
- `app.manifest` -- declares Per-Monitor V2 DPI awareness. Without this,
  the embedded Qt viewport double-scales relative to the rest of the WPF
  window on non-100%-DPI monitors.

## Building and running

This app is platform-specific (`x64`/`x86`, not `AnyCPU`) because
`dwgcore.dll` is a native dependency and the host process's bitness must
match it. Build `dwgcore.dll` for the matching bitness *first* (see
`../dwgcore/README.md`, including the `windeployqt` step) -- this project's
`.csproj` copies whatever is currently in `../dwgcore/build/Release`
(x64) or `../dwgcore/build-x86/Release` (x86) into its own output
directory, so it needs that to already exist and be current.

```
dotnet build -p:Platform=x64 -c Release
dotnet build -p:Platform=x86 -c Release

bin\x64\Release\net8.0-windows\win-x64\DwgCoreTest.exe
bin\x64\Release\net8.0-windows\win-x64\DwgCoreTest.exe ..\dwgviewer\sample_data\basic.dxf
```

## A note on the "hang on first launch after a rebuild" symptom

If the window doesn't appear for what feels like a long time right after a
fresh `dwgcore.dll` rebuild, that's very likely Windows Defender's
real-time scan of the newly-built (so newly-hashed) DLL and its Qt
dependencies, not a deadlock -- it happened repeatedly during this app's
own development and resolved on the next launch of the same binaries. See
`../dwgcore/CLAUDE.md`'s note on `interop_smoke_test.cpp` for how to tell
the difference conclusively (it isolates the native DLL from this WPF
layer entirely).
