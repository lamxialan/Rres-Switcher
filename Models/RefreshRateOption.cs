namespace RRes.Models
{
    public class RefreshRateOption
    {
        public int Value { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public bool IsAuto { get; set; }

        public override string ToString() => DisplayName;
    }
}
