using System;
using System.ComponentModel;
using System.Windows;
using RRes.Core.Utils;
using RRes.ViewModels;

namespace RRes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SystemTrayManager? _trayManager;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                var vm = DataContext as MainViewModel ?? new MainViewModel();
                DataContext = vm;

                // Inisialisasi integrasi System Tray (NotifyIcon) Windows
                _trayManager = new SystemTrayManager(this, () =>
                {
                    vm.RevertToNativeCommand.Execute(null);
                });

                vm.RequestMinimizeToTrayAction = () =>
                {
                    _trayManager?.MinimizeToTray(true);
                };

                vm.RequestShowNotificationAction = (title, msg) =>
                {
                    _trayManager?.ShowNotification(title, msg);
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RRes MainWindow Error]: {ex}");
                MessageBox.Show(
                    $"Gagal memuat komponen antarmuka RRes:\n\n{ex.Message}\n\nDetail:\n{ex.StackTrace}",
                    "RRes - UI Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                throw;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _trayManager?.Dispose();
            base.OnClosed(e);
        }
    }
}