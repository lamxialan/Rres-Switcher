using System;
using System.IO;
using System.Windows;

namespace RRes.Core.Utils
{
    public class SystemTrayManager : IDisposable
    {
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private readonly Window _mainWindow;
        private readonly Action _onForceRevertNative;

        public bool IsTrayActive => _notifyIcon != null && _notifyIcon.Visible;

        public SystemTrayManager(Window mainWindow, Action onForceRevertNative)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            _onForceRevertNative = onForceRevertNative ?? throw new ArgumentNullException(nameof(onForceRevertNative));

            InitializeTray();
        }

        private void InitializeTray()
        {
            try
            {
                _notifyIcon = new System.Windows.Forms.NotifyIcon
                {
                    Text = "RRes - Valorant Resolution Switcher",
                    Visible = false
                };

                // Load Icon Resmi Aplikasi dari Resource Stream
                try
                {
                    var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"))?.Stream;
                    if (iconStream != null)
                    {
                        _notifyIcon.Icon = new System.Drawing.Icon(iconStream);
                    }
                    else
                    {
                        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
                        if (File.Exists(iconPath))
                        {
                            _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                        }
                        else
                        {
                            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[RRes Tray Icon Load Warning]: {ex.Message}");
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }

                // Buat Context Menu
                var contextMenu = new System.Windows.Forms.ContextMenuStrip();

                var openItem = new System.Windows.Forms.ToolStripMenuItem("Buka RRes");
                openItem.Font = new System.Drawing.Font(openItem.Font, System.Drawing.FontStyle.Bold);
                openItem.Click += (s, e) => RestoreFromTray();

                var revertItem = new System.Windows.Forms.ToolStripMenuItem("Force Revert Native (Reset)");
                revertItem.Click += (s, e) =>
                {
                    _onForceRevertNative?.Invoke();
                    ShowNotification("Resolusi Dipulihkan", "Resolusi monitor dan config telah dikembalikan ke Native.");
                };

                var separator = new System.Windows.Forms.ToolStripSeparator();

                var exitItem = new System.Windows.Forms.ToolStripMenuItem("Tutup RRes (Exit)");
                exitItem.Click += (s, e) =>
                {
                    Dispose();
                    System.Windows.Application.Current.Shutdown();
                };

                contextMenu.Items.Add(openItem);
                contextMenu.Items.Add(revertItem);
                contextMenu.Items.Add(separator);
                contextMenu.Items.Add(exitItem);

                _notifyIcon.ContextMenuStrip = contextMenu;
                _notifyIcon.DoubleClick += (s, e) => RestoreFromTray();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RRes SystemTray Initialization Failed]: {ex.Message}");
            }
        }

        public void MinimizeToTray(bool notify = true)
        {
            if (_notifyIcon == null) return;

            _mainWindow.Dispatcher.Invoke(() =>
            {
                _mainWindow.WindowState = WindowState.Minimized;
                _mainWindow.Hide();
            });

            _notifyIcon.Visible = true;

            if (notify)
            {
                _notifyIcon.ShowBalloonTip(
                    3000,
                    "RRes Berjalan di Background",
                    "RRes tetap memantau Valorant di system tray. Klik 2x untuk membuka kembali.",
                    System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        public void RestoreFromTray()
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
                _mainWindow.Focus();
            });
        }

        public void ShowNotification(string title, string message)
        {
            if (_notifyIcon != null && _notifyIcon.Visible)
            {
                _notifyIcon.ShowBalloonTip(2500, title, message, System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}
