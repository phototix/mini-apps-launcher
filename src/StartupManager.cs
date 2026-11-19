using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MiniAppsLauncher
{
    internal static class StartupManager
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "MiniAppsLauncher";

        public static string Command
        {
            get { return "\"" + Application.ExecutablePath + "\" --tray"; }
        }

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (key == null)
                    {
                        return false;
                    }
                    string value = key.GetValue(ValueName) as string;
                    return !string.IsNullOrWhiteSpace(value);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not read the startup entry", ex);
                return false;
            }
        }

        public static bool SetEnabled(bool enabled, out string error)
        {
            error = null;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null)
                    {
                        error = "The Windows startup registry key is not accessible.";
                        return false;
                    }

                    if (enabled)
                    {
                        key.SetValue(ValueName, Command, RegistryValueKind.String);
                    }
                    else if (key.GetValue(ValueName) != null)
                    {
                        key.DeleteValue(ValueName, false);
                    }
                }

                Log.Info("Start with Windows = " + enabled);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Could not update the startup entry", ex);
                error = ex.Message;
                return false;
            }
        }
    }
}
