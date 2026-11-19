using System;
using System.Threading;
using System.Windows.Forms;

namespace MiniAppsLauncher
{
    internal static class Program
    {
        private const string MutexName = @"Local\MiniAppsLauncher.SingleInstance";

        [STAThread]
        private static void Main(string[] args)
        {
            if (SelfTest.IsRequested(args))
            {
                Environment.ExitCode = SelfTest.Run();
                return;
            }

            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    SignalRunningInstance();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += OnThreadException;
                AppDomain.CurrentDomain.UnhandledException += OnDomainException;

                Log.Info("Starting Mini Apps Launcher " + Application.ProductVersion);

                using (LauncherContext context = new LauncherContext())
                {
                    Application.Run(context);
                }

                Log.Info("Exited");
                GC.KeepAlive(mutex);
            }
        }

        private static void SignalRunningInstance()
        {
            try
            {
                using (EventWaitHandle handle = EventWaitHandle.OpenExisting(@"Local\MiniAppsLauncher.Show"))
                {
                    handle.Set();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not signal the running instance", ex);
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log.Error("Unhandled UI exception", e.Exception);
            MessageBox.Show(
                "Mini Apps Launcher hit an error:\n\n" + e.Exception.Message +
                "\n\nSee the log for details:\n" + Log.FileName,
                "Mini Apps Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void OnDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error("Unhandled exception", e.ExceptionObject as Exception);
        }
    }
}
