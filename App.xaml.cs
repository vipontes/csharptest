using System.Windows;

namespace DwgCoreTest;

public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        // Mandatory, not optional cleanup -- see dwgcore_api.h's
        // DwgCore_Shutdown: there is no DllMain-based teardown, since
        // joining dwgcore's Qt worker thread during DLL_PROCESS_DETACH is
        // unsafe.
        NativeMethods.DwgCore_Shutdown();
        base.OnExit(e);
    }
}
