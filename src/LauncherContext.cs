using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace MiniAppsLauncher
{
    internal sealed class LauncherContext : ApplicationContext
    {
        private const string ShowEventName = @"Local\MiniAppsLauncher.Show";

        private readonly NotifyIcon _tray;
        private readonly System.Windows.Forms.Timer _pulse;
        private readonly EventWaitHandle _showEvent;
        private readonly RegisteredWaitHandle _showRegistration;

        private AppConfig _config;
        private ContextMenuStrip _menu;
        private FileSystemWatcher _watcher;
        private volatile bool _reloadPending;
        private volatile bool _showPending;
        private bool _disposed;

        public LauncherContext()
        {
            _config = ConfigStore.Load();

            _tray = new NotifyIcon();
            _tray.Icon = LoadTrayIcon();
            _tray.Text = "Mini Apps Launcher";
            _tray.Visible = true;
            _menu = BuildMenu();
            _tray.ContextMenuStrip = _menu;
            _tray.MouseClick += OnTrayMouseClick;

            _pulse = new System.Windows.Forms.Timer();
            _pulse.Interval = 750;
            _pulse.Tick += OnPulse;
            _pulse.Start();

            bool createdNew;
            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName, out createdNew);
            _showRegistration = ThreadPool.RegisterWaitForSingleObject(
                _showEvent, OnShowSignal, null, Timeout.Infinite, false);

            StartWatcher();
            Log.Info("Launcher ready with " + _config.Apps.Count + " app(s)");
        }

        private void OnTrayMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowMenu();
            }
        }

        private void ShowMenu()
        {
            if (_disposed || _menu == null)
            {
                return;
            }

            try
            {
                RefreshStartupState();
                _menu.Show(Cursor.Position);
            }
            catch (Exception ex)
            {
                Log.Error("Could not show the tray menu", ex);
            }
        }

        private ContextMenuStrip BuildMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.ImageScalingSize = new Size(16, 16);
            menu.RenderMode = ToolStripRenderMode.System;
            menu.Opening += delegate { RefreshStartupState(); };

            int enabledCount = 0;
            foreach (AppEntry app in _config.Apps)
            {
                if (!app.Enabled)
                {
                    continue;
                }

                enabledCount++;
                ToolStripMenuItem item = new ToolStripMenuItem();
                item.Text = app.DisplayName;
                item.Tag = app.Id;
                item.Image = LoadAppIcon(app);

                if (app.Exists)
                {
                    item.ToolTipText = app.ExpandedPath;
                    AppEntry captured = app;
                    item.Click += delegate { Launch(captured); };
                }
                else
                {
                    item.Enabled = false;
                    item.Text = app.DisplayName + " (missing)";
                    item.ToolTipText = "Not found: " + app.ExpandedPath;
                }

                menu.Items.Add(item);
            }

            if (enabledCount == 0)
            {
                ToolStripMenuItem empty = new ToolStripMenuItem("No apps configured");
                empty.Enabled = false;
                menu.Items.Add(empty);
            }

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem configItem = new ToolStripMenuItem("Open config folder");
            configItem.Click += delegate { OpenConfigFolder(); };
            menu.Items.Add(configItem);

            ToolStripMenuItem reloadItem = new ToolStripMenuItem("Reload apps");
            reloadItem.Click += delegate { ReloadConfig(true); };
            menu.Items.Add(reloadItem);

            ToolStripMenuItem startupItem = new ToolStripMenuItem("Start with Windows");
            startupItem.Name = "startup";
            startupItem.CheckOnClick = true;
            startupItem.Checked = StartupManager.IsEnabled();
            startupItem.Click += OnStartupToggle;
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += delegate { ShowAbout(); };
            menu.Items.Add(aboutItem);

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += delegate { ExitLauncher(); };
            menu.Items.Add(exitItem);

            return menu;
        }

        private void RefreshStartupState()
        {
            if (_menu == null)
            {
                return;
            }

            ToolStripItem[] items = _menu.Items.Find("startup", false);
            if (items.Length > 0)
            {
                ToolStripMenuItem item = items[0] as ToolStripMenuItem;
                if (item != null)
                {
                    item.Checked = StartupManager.IsEnabled();
                }
            }
        }

        private void OnStartupToggle(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item == null)
            {
                return;
            }

            string error;
            if (!StartupManager.SetEnabled(item.Checked, out error))
            {
                item.Checked = !item.Checked;
                _tray.ShowBalloonTip(
                    5000,
                    "Could not change the startup setting",
                    error,
                    ToolTipIcon.Error);
            }
        }

        private void Launch(AppEntry app)
        {
            string message;
            if (!ProcessLauncher.TryLaunch(app, out message))
            {
                _tray.ShowBalloonTip(
                    5000,
                    "Could not start " + app.DisplayName,
                    message,
                    ToolTipIcon.Error);
            }
            else if (!string.IsNullOrEmpty(message))
            {
                Log.Info(app.DisplayName + ": " + message);
            }
        }

        private void OpenConfigFolder()
        {
            try
            {
                string configPath = ConfigStore.ConfigPath;
                if (File.Exists(configPath))
                {
                    Process.Start("explorer.exe", "/select,\"" + configPath + "\"");
                }
                else
                {
                    Directory.CreateDirectory(ConfigStore.ConfigDirectory);
                    Process.Start("explorer.exe", "\"" + ConfigStore.ConfigDirectory + "\"");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not open the config folder", ex);
                _tray.ShowBalloonTip(5000, "Could not open the config folder", ex.Message, ToolTipIcon.Error);
            }
        }

        private void ReloadConfig(bool announce)
        {
            try
            {
                _config = ConfigStore.Load();
                ReplaceMenu();

                if (announce)
                {
                    _tray.ShowBalloonTip(
                        2500,
                        "Mini Apps Launcher",
                        "Apps reloaded (" + _config.Apps.Count + ").",
                        ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Config reload failed", ex);
                _tray.ShowBalloonTip(6000, "Config error", ex.Message, ToolTipIcon.Error);
            }
        }

        private void ReplaceMenu()
        {
            ContextMenuStrip previous = _menu;
            _menu = BuildMenu();
            _tray.ContextMenuStrip = _menu;

            if (previous != null)
            {
                previous.Visible = false;
                previous.Dispose();
            }
        }

        private void StartWatcher()
        {
            try
            {
                string path = ConfigStore.ConfigPath;
                string directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory))
                {
                    return;
                }

                Directory.CreateDirectory(directory);
                _watcher = new FileSystemWatcher(directory, Path.GetFileName(path));
                _watcher.NotifyFilter = NotifyFilters.LastWrite
                    | NotifyFilters.Size
                    | NotifyFilters.FileName
                    | NotifyFilters.CreationTime;
                _watcher.Changed += OnConfigChanged;
                _watcher.Created += OnConfigChanged;
                _watcher.Deleted += OnConfigChanged;
                _watcher.Renamed += OnConfigRenamed;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Log.Error("Could not watch the config file", ex);
            }
        }

        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            _reloadPending = true;
        }

        private void OnConfigRenamed(object sender, RenamedEventArgs e)
        {
            _reloadPending = true;
        }

        private void OnShowSignal(object state, bool timedOut)
        {
            if (!timedOut)
            {
                _showPending = true;
            }
        }

        private void OnPulse(object sender, EventArgs e)
        {
            if (_showPending)
            {
                _showPending = false;
                ShowMenu();
            }

            if (_reloadPending)
            {
                _reloadPending = false;
                ReloadConfig(false);
            }
        }

        private void ShowAbout()
        {
            string text = "Mini Apps Launcher " + Application.ProductVersion + "\n\n"
                + "Apps configured: " + _config.Apps.Count + "\n"
                + "Config: " + ConfigStore.ConfigPath + "\n"
                + "Log: " + Log.FileName + "\n\n"
                + "Edit apps.json to add or change apps, then choose Reload apps.";
            MessageBox.Show(
                text,
                "About Mini Apps Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ExitLauncher()
        {
            _tray.Visible = false;
            ExitThread();
        }

        private static Icon LoadTrayIcon()
        {
            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null)
                {
                    return icon;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not load the tray icon", ex);
            }

            return SystemIcons.Application;
        }

        private static Image LoadAppIcon(AppEntry app)
        {
            try
            {
                string iconPath = app.ExpandedIconPath;
                if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                {
                    using (Icon icon = new Icon(iconPath, 16, 16))
                    {
                        return icon.ToBitmap();
                    }
                }

                if (app.Exists)
                {
                    Icon extracted = Icon.ExtractAssociatedIcon(app.ExpandedPath);
                    if (extracted != null)
                    {
                        return extracted.ToBitmap();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not load the icon for " + app.DisplayName, ex);
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                try
                {
                    _pulse.Stop();
                    _pulse.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error("Timer dispose failed", ex);
                }

                try
                {
                    if (_watcher != null)
                    {
                        _watcher.EnableRaisingEvents = false;
                        _watcher.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Watcher dispose failed", ex);
                }

                try
                {
                    if (_showRegistration != null)
                    {
                        _showRegistration.Unregister(null);
                    }
                }
                catch (Exception)
                {
                }

                try
                {
                    if (_showEvent != null)
                    {
                        _showEvent.Dispose();
                    }
                }
                catch (Exception)
                {
                }

                try
                {
                    _tray.Visible = false;
                    _tray.Dispose();
                }
                catch (Exception)
                {
                }

                try
                {
                    if (_menu != null)
                    {
                        _menu.Dispose();
                    }
                }
                catch (Exception)
                {
                }
            }

            base.Dispose(disposing);
        }
    }
}
