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

                StateChanged += MainWindow_StateChanged;
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

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleWindowState();
            }
            else if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ToggleWindowState()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                if (MaximizeIconText != null) MaximizeIconText.Text = "🗗";
                if (RootWindowBorder != null)
                {
                    RootWindowBorder.BorderThickness = new Thickness(0);
                    RootWindowBorder.Margin = new Thickness(7);
                }
            }
            else
            {
                if (MaximizeIconText != null) MaximizeIconText.Text = "▢";
                if (RootWindowBorder != null)
                {
                    RootWindowBorder.BorderThickness = new Thickness(1);
                    RootWindowBorder.Margin = new Thickness(0);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _trayManager?.Dispose();
            base.OnClosed(e);
        }
    }
}