using System;

namespace RRes.Models
{
    public class CrosshairProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Tag { get; set; } = "Pro";
        public string ColorHex { get; set; } = "#38BDF8";
        public bool IsCustom { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
