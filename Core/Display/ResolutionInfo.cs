using System;

namespace RRes.Core.Display
{
    public class ResolutionInfo
    {
        public int Width { get; }
        public int Height { get; }
        public int RefreshRate { get; }

        public ResolutionInfo(int width, int height, int refreshRate = 0)
        {
            Width = width;
            Height = height;
            RefreshRate = refreshRate;
        }

        public string AspectRatio
        {
            get
            {
                int gcd = GetGreatestCommonDivisor(Width, Height);
                int rWidth = Width / gcd;
                int rHeight = Height / gcd;

                // Typical display ratio representations
                if (rWidth == 8 && rHeight == 5) return "16:10";
                if (rWidth == 16 && rHeight == 9) return "16:9";
                if (rWidth == 4 && rHeight == 3) return "4:3";
                if (rWidth == 5 && rHeight == 4) return "5:4";
                if (rWidth == 21 && rHeight == 9) return "21:9";
                if (rWidth == 32 && rHeight == 9) return "32:9";

                return $"{rWidth}:{rHeight}";
            }
        }

        public override string ToString()
        {
            return RefreshRate > 0 ? $"{Width}x{Height} @ {RefreshRate}Hz ({AspectRatio})" : $"{Width}x{Height} ({AspectRatio})";
        }

        private static int GetGreatestCommonDivisor(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }
    }
}
