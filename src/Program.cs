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

            string launchId = GetLaunchId(args);
            if (!string.IsNullOrEmpty(launchId))
            {
                Environment.ExitCode = LaunchFromCommandLine(launchId);
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

        private static string GetLaunchId(string[] args)
        {
            if (args == null)
            {
                return null;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--launch", StringComparison.OrdinalIgnoreCase)
                    && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static int LaunchFromCommandLine(string id)
        {
            try
            {
                AppConfig config = ConfigStore.Load();
                foreach (AppEntry app in config.Apps)
                {
                    if (!string.Equals(app.Id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string message;
                    bool launched = ProcessLauncher.TryLaunch(app, out message);
                    Log.Info("Command-line launch of " + app.DisplayName + ": "
                        + (launched ? "OK" : "FAILED")
                        + (string.IsNullOrEmpty(message) ? "" : " (" + message + ")"));
                    return launched ? 0 : 2;
                }

                Log.Error("Command-line launch: no app with id '" + id + "'");
                return 3;
            }
            catch (Exception ex)
            {
                Log.Error("Command-line launch failed", ex);
                return 1;
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
