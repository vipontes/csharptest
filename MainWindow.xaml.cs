using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace DwgCoreTest;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    // Optional `DwgCoreTest.exe path\to\file.dxf` command-line argument,
    // mirroring dwgviewer/src/main.cpp's own argv[1] convenience -- lets
    // this harness be driven non-interactively (e.g. for verification)
    // without needing to automate the Open file dialog.
    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length < 2) return;
        await LoadFileAsync(args[1]);
    }

    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Drawing files (*.dwg;*.dxf)|*.dwg;*.dxf|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        await LoadFileAsync(dialog.FileName);
    }

    private async Task LoadFileAsync(string path)
    {
        IntPtr handle = Viewport.ViewHandle;
        if (handle == IntPtr.Zero) return;

        StatusText.Text = "Loading...";

        // DwgCore_LoadFile blocks until parsing finishes (see
        // dwgcore_api.h) -- run it off the UI thread so a large file
        // doesn't freeze the window.
        bool ok = await Task.Run(() => NativeMethods.DwgCore_LoadFile(handle, path));

        StatusText.Text = ok
            ? $"Loaded {Path.GetFileName(path)}"
            : $"Failed: {NativeMethods.GetLastError(handle)}";
    }

    private void ZoomFitButton_Click(object sender, RoutedEventArgs e)
    {
        if (Viewport.ViewHandle != IntPtr.Zero)
        {
            NativeMethods.DwgCore_ZoomFit(Viewport.ViewHandle);
        }
    }
}
