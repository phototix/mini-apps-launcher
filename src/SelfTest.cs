using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MiniAppsLauncher
{
    internal static class SelfTest
    {
        public static bool IsRequested(string[] args)
        {
            if (args == null)
            {
                return false;
            }

            foreach (string arg in args)
            {
                if (string.Equals(arg, "--selftest", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static int Run()
        {
            StringBuilder report = new StringBuilder();
            int warnings = 0;

            try
            {
                report.AppendLine("Mini Apps Launcher self-test");
                report.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                report.AppendLine("Exe: " + Application.ExecutablePath);
                report.AppendLine("Config: " + ConfigStore.ConfigPath);
                report.AppendLine("Log: " + Log.FileName);
                report.AppendLine();

                AppConfig config = ConfigStore.Load();
                report.AppendLine("Apps configured: " + config.Apps.Count);
                report.AppendLine();

                foreach (AppEntry app in config.Apps)
                {
                    bool exists = app.Exists;
                    string workingDirectory = app.ExpandedWorkingDirectory;
                    bool workingDirectoryOk = !string.IsNullOrWhiteSpace(workingDirectory)
                        && Directory.Exists(workingDirectory);
                    bool iconOk = false;

                    try
                    {
                        string iconPath = app.ExpandedIconPath;
                        if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                        {
                            using (Icon icon = new Icon(iconPath, 16, 16))
                            {
                                iconOk = icon != null;
                            }
                        }
                        else if (exists)
                        {
                            using (Icon icon = Icon.ExtractAssociatedIcon(app.ExpandedPath))
                            {
                                iconOk = icon != null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        report.AppendLine("  icon error: " + ex.Message);
                    }

                    if (app.Enabled && !exists)
                    {
                        warnings++;
                    }

                    report.AppendLine("- " + app.DisplayName + " (" + app.Id + ")");
                    report.AppendLine("    path: " + app.ExpandedPath + " -> " + (exists ? "OK" : "MISSING"));
                    report.AppendLine("    workdir: " + workingDirectory + " -> " + (workingDirectoryOk ? "OK" : "MISSING"));
                    report.AppendLine("    icon: " + (iconOk ? "OK" : "none"));
                    report.AppendLine("    enabled: " + app.Enabled
                        + ", singleInstance: " + app.SingleInstance
                        + ", runAsAdmin: " + app.RunAsAdmin);
                }

                report.AppendLine();
                report.AppendLine("Start with Windows: " + StartupManager.IsEnabled());
                report.AppendLine();
                report.AppendLine("RESULT: PASS"
                    + (warnings == 0 ? "" : " (" + warnings + " enabled app(s) missing - this is expected on a fresh machine)"));
            }
            catch (Exception ex)
            {
                report.AppendLine("RESULT: FAIL");
                report.AppendLine(ex.ToString());
                WriteReport(report.ToString());
                return 1;
            }

            WriteReport(report.ToString());
            return 0;
        }

        private static void WriteReport(string text)
        {
            try
            {
                string path = Path.Combine(Path.GetTempPath(), "MiniAppsLauncher.selftest.txt");
                File.WriteAllText(path, text, new UTF8Encoding(false));
            }
            catch
            {
            }
        }
    }
}
