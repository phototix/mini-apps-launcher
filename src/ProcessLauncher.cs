using System;
using System.Diagnostics;
using System.IO;

namespace MiniAppsLauncher
{
    internal static class ProcessLauncher
    {
        public static bool TryLaunch(AppEntry app, out string message)
        {
            message = null;
            if (app == null)
            {
                message = "App entry is missing.";
                return false;
            }

            string executable = app.ExpandedPath;
            if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            {
                message = "Executable not found:\n" + executable;
                return false;
            }

            if (app.SingleInstance)
            {
                Process running = FindRunning(executable);
                if (running != null)
                {
                    try
                    {
                        if (running.MainWindowHandle != IntPtr.Zero)
                        {
                            WindowTools.BringToFront(running.MainWindowHandle);
                            message = "Focused the running window.";
                        }
                        else
                        {
                            message = "It is already running.";
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Could not focus the running instance", ex);
                    }
                }
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = executable;
                startInfo.Arguments = app.Arguments == null ? "" : app.Arguments;
                startInfo.UseShellExecute = true;

                string workingDirectory = app.ExpandedWorkingDirectory;
                if (!string.IsNullOrWhiteSpace(workingDirectory) && Directory.Exists(workingDirectory))
                {
                    startInfo.WorkingDirectory = workingDirectory;
                }

                if (app.RunAsAdmin)
                {
                    startInfo.Verb = "runas";
                }

                Process.Start(startInfo);
                Log.Info("Launched " + app.DisplayName + " -> " + executable);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Launch failed for " + executable, ex);
                message = ex.Message;
                return false;
            }
        }

        private static Process FindRunning(string executable)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(executable);
                Process[] candidates = Process.GetProcessesByName(name);
                string fullPath = Path.GetFullPath(executable);

                foreach (Process candidate in candidates)
                {
                    try
                    {
                        string module = candidate.MainModule.FileName;
                        if (string.Equals(Path.GetFullPath(module), fullPath, StringComparison.OrdinalIgnoreCase))
                        {
                            return candidate;
                        }
                    }
                    catch (Exception)
                    {
                        // Access denied for elevated or exiting processes; ignore them.
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Process lookup failed", ex);
            }

            return null;
        }
    }
}
