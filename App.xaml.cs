using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using RRes.Views;

namespace RRes
{
    /// <summary>
    /// Interaction logic for App.xaml with global unhandled exception handling, crash log persistence, and animated splash screen.
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public App()
        {
            // 1. Tangkap unhandled exception pada thread UI WPF sejak awal siklus hidup aplikasi
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // 2. Tangkap exception domain tingkat dasar (background thread / unmanaged interop)
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // 3. Tangkap exception pada task asynchronous yang tidak teramati
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                // Muat konfigurasi tema & font pengguna sejak awal
                try
                {
                    var settingsService = new RRes.Core.Utils.AppSettingsService();
                    var settings = settingsService.LoadSettings();
                    RRes.Core.Utils.ThemeManager.ApplyTheme(settings.ThemeMode, settings.AccentColor, settings.FontFamily, settings.BaseFontSize);
                }
                catch { }

                // Set ShutdownMode ke OnExplicitShutdown sementara waktu agar transisi splash tidak menutup proses
                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // 1. Buat dan tampilkan jendela Splash Screen di tengah layar
                var splash = new SplashScreenWindow();
                splash.Show();

                // 2. Jalankan animasi splash screen dengan timeout pengaman (maksimal 2.5 detik)
                // agar aplikasi tidak pernah freeze jika akselerasi grafis layar bermasalah
                var splashAnimationTask = splash.PlaySplashAnimationAsync();
                await Task.WhenAny(splashAnimationTask, Task.Delay(2500));

                // 3. Buka MainWindow, fokuskan ke layar utama, dan ubah ShutdownMode
                var mainWindow = new MainWindow();
                this.MainWindow = mainWindow;
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;

                mainWindow.Show();
                mainWindow.Activate();

                // 4. Tutup jendela splash screen
                try
                {
                    splash.Close();
                }
                catch
                {
                    // Abaikan jika splash sudah tertutup
                }
            }
            catch (Exception ex)
            {
                HandleFatalException(ex, "Startup Fatal Exception");
                this.Shutdown(-1);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleFatalException(e.Exception, "WPF Dispatcher UI Exception");
            // Tandai sudah ditangani agar proses tidak crash mendadak dengan kode 0xe0434352
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleFatalException(ex, "AppDomain Unhandled Exception");
            }
            else
            {
                HandleFatalException(new Exception($"Objek non-exception dilempar: {e.ExceptionObject}"), "AppDomain Unhandled Exception");
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleFatalException(e.Exception, "Unobserved Task Exception");
            e.SetObserved();
        }

        private static void HandleFatalException(Exception? ex, string source)
        {
            string message = ex?.Message ?? "Terjadi kesalahan tidak dikenal.";
            string details = ex?.ToString() ?? "Tidak ada detail stack trace.";

            // 1. Cetak ke terminal / debugger
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"[RRes Error - {source}]: {message}");
                Console.Error.WriteLine(details);
                Console.ResetColor();

                Trace.TraceError($"[RRes Error - {source}]: {details}");
            }
            catch { }

            // 2. Simpan secara persisten ke crash.log di AppData agar user/developer bisa melacak error
            string? logFilePath = null;
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrWhiteSpace(appData))
                {
                    appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                }
                if (string.IsNullOrWhiteSpace(appData))
                {
                    appData = AppDomain.CurrentDomain.BaseDirectory;
                }

                string rresDir = Path.Combine(appData, "RRes");
                if (!Directory.Exists(rresDir))
                {
                    Directory.CreateDirectory(rresDir);
                }

                logFilePath = Path.Combine(rresDir, "crash.log");
                File.AppendAllText(logFilePath, $"========================================\r\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\r\nPesan: {message}\r\nDetail:\r\n{details}\r\n\r\n");
            }
            catch { }

            // 3. Tampilkan pesan error informatif kepada pengguna alih-alih crash diam-diam
            string userFriendlyMessage = $"Terjadi kesalahan pada RRes ({source}):\n\n{message}\n\nDetail:\n{details}";
            if (!string.IsNullOrEmpty(logFilePath))
            {
                userFriendlyMessage += $"\n\nCatatan crash tersimpan di:\n{logFilePath}";
            }

            try
            {
                System.Windows.MessageBox.Show(
                    userFriendlyMessage,
                    "RRes - Fatal Error / Exception Handler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // Fallback ke WinForms MessageBox jika subsistem WPF Visual gagal
                try
                {
                    System.Windows.Forms.MessageBox.Show(
                        userFriendlyMessage,
                        "RRes - Fatal Error",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error);
                }
                catch
                {
                    // Fail-safe jika Win32 dialog pun gagal
                }
            }
        }
    }
}
