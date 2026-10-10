using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace RRes.Core.Utils
{
    public static class ImageTransparencyHelper
    {
        /// <summary>
        /// Mengonversi BitmapSource yang memiliki latar belakang hitam menjadi transparan (Alpha = 0 untuk piksel hitam).
        /// Menghasilkan siluet lambang putih yang bersih dan halus (anti-aliased).
        /// </summary>
        public static BitmapSource MakeBlackTransparent(BitmapSource source, byte threshold = 25)
        {
            // Pastikan format Bgra32 untuk akses byte per piksel
            FormatConvertedBitmap formatted = new FormatConvertedBitmap();
            formatted.BeginInit();
            formatted.Source = source;
            formatted.DestinationFormat = System.Windows.Media.PixelFormats.Bgra32;
            formatted.EndInit();

            int width = formatted.PixelWidth;
            int height = formatted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            formatted.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];

                // Hitung luminansi/kecerahan piksel
                int lum = (int)(0.299 * r + 0.587 * g + 0.114 * b);

                if (lum < threshold)
                {
                    pixels[i + 3] = 0; // Transparan penuh
                }
                else
                {
                    // Haluskan transisi tepi logo putih
                    int alpha = Math.Min(255, (int)((lum - threshold) * (255.0 / (255 - threshold))));
                    pixels[i] = 255;     // B
                    pixels[i + 1] = 255; // G
                    pixels[i + 2] = 255; // R
                    pixels[i + 3] = (byte)alpha; // Alpha
                }
            }

            return BitmapSource.Create(
                width,
                height,
                formatted.DpiX,
                formatted.DpiY,
                System.Windows.Media.PixelFormats.Bgra32,
                null,
                pixels,
                stride);
        }

        /// <summary>
        /// Memuat gambar dari URI resource atau disk, kemudian mengembalikan BitmapSource transparan.
        /// </summary>
        public static BitmapSource LoadTransparentLogo()
        {
            // 1. Coba ambil splash_logo.png via Resource Stream
            try
            {
                var uriPng = new Uri("pack://application:,,,/Assets/splash_logo.png", UriKind.Absolute);
                var streamInfo = System.Windows.Application.GetResourceStream(uriPng);
                if (streamInfo != null)
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = streamInfo.Stream;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
            }
            catch { }

            // 2. Coba ambil splash_logo.jpg via Resource Stream
            try
            {
                var uriJpg = new Uri("pack://application:,,,/Assets/splash_logo.jpg", UriKind.Absolute);
                var streamInfo = System.Windows.Application.GetResourceStream(uriJpg);
                if (streamInfo != null)
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = streamInfo.Stream;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    return MakeBlackTransparent(bi);
                }
            }
            catch { }

            // 3. Fallback disk jika file berada di folder Assets di BaseDirectory
            try
            {
                string diskPng = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "splash_logo.png");
                if (File.Exists(diskPng))
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(diskPng, UriKind.Absolute);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
            }
            catch { }

            try
            {
                string diskJpg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "splash_logo.jpg");
                if (File.Exists(diskJpg))
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(diskJpg, UriKind.Absolute);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    return MakeBlackTransparent(bi);
                }
            }
            catch { }

            // 4. Fallback aman 1x1 transparan agar jendela splash tidak crash
            return BitmapSource.Create(1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, new byte[4], 4);
        }
    }
}
