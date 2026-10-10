using System;

namespace RRes.Models
{
    public enum RawAccelCurveType
    {
        Linear,
        Natural,
        Jump,
        Off
    }

    public class RawAccelCurvePreset
    {
        public RawAccelCurveType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string BadgeText { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ModeString { get; set; } = "linear";
        public double Acceleration { get; set; } = 0.05;
        public double Cap { get; set; } = 1.5;
        public double Offset { get; set; } = 0.0;
        public double Exponent { get; set; } = 2.0;
        public double Smooth { get; set; } = 0.0;
        public double DecayRate { get; set; } = 0.1;

        public override string ToString() => Name;
    }
}
