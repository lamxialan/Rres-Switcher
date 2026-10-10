namespace RRes.Models
{
    public class MonitorInfo
    {
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceString { get; set; } = string.Empty;
        public string MonitorName { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int Index { get; set; }

        public string DisplayName
        {
            get
            {
                string name = !string.IsNullOrWhiteSpace(MonitorName) ? MonitorName : DeviceString;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"Monitor {Index + 1}";
                }
                return IsPrimary
                    ? $"{name} ({DeviceName}) [Utama]"
                    : $"{name} ({DeviceName})";
            }
        }

        public override string ToString() => DisplayName;
    }
}
