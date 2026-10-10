using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using RRes.Core.Utils;

namespace RRes.Views
{
    /// <summary>
    /// Jendela Splash Screen transparan dengan animasi sinematik Fade-In, Scale-In, Jeda, dan Fade-Out
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
            LoadLogo();
        }

        private void LoadLogo()
        {
            try
            {
                var bitmap = ImageTransparencyHelper.LoadTransparentLogo();
                LogoImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SplashScreen Logo Load Warning]: {ex.Message}");
            }
        }

        /// <summary>
        /// Menjalankan rangkaian animasi splash screen dan mengembalikan Task yang selesai ketika animasi tuntas.
        /// </summary>
        public Task PlaySplashAnimationAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            var storyboard = new Storyboard();

            // 1. Animasi Fade-In (0.0s - 0.6s)
            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.6),
                BeginTime = TimeSpan.FromSeconds(0.0),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, RootContainer);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(fadeIn);

            // 2. Animasi Scale-In Dinamis (0.0s - 0.6s)
            var scaleX = new DoubleAnimation
            {
                From = 0.92,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.6),
                BeginTime = TimeSpan.FromSeconds(0.0),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleX, RootScale);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath(System.Windows.Media.ScaleTransform.ScaleXProperty));
            storyboard.Children.Add(scaleX);

            var scaleY = new DoubleAnimation
            {
                From = 0.92,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.6),
                BeginTime = TimeSpan.FromSeconds(0.0),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleY, RootScale);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath(System.Windows.Media.ScaleTransform.ScaleYProperty));
            storyboard.Children.Add(scaleY);

            // 3. Jeda diam (~0.8s), lalu Animasi Fade-Out (1.4s - 1.9s)
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(0.5),
                BeginTime = TimeSpan.FromSeconds(1.4),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(fadeOut, RootContainer);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(fadeOut);

            storyboard.Completed += (s, e) =>
            {
                tcs.TrySetResult(true);
            };

            storyboard.Begin();

            return tcs.Task;
        }
    }
}
