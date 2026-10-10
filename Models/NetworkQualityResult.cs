using System;

namespace RRes.Models
{
    public class NetworkQualityResult
    {
        public string ServerName { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int PacketsSent { get; set; }
        public int PacketsReceived { get; set; }
        public double PacketLossPercent { get; set; }
        public double AverageLatencyMs { get; set; }
        public double MinLatencyMs { get; set; }
        public double MaxLatencyMs { get; set; }
        public double JitterMs { get; set; }
        public bool IsSuccess => PacketsReceived > 0;

        public string QualityStatus
        {
            get
            {
                if (!IsSuccess || PacketLossPercent > 5.0) return "Koneksi Buruk (Packet Loss)";
                if (JitterMs >= 8.0 || PacketLossPercent > 0.0) return "Jitter Tinggi (Waspada)";
                if (AverageLatencyMs > 120.0) return "Latensi Tinggi";
                return "Stabil (Siap Ranked)";
            }
        }

        public string QualityColor
        {
            get
            {
                if (!IsSuccess || PacketLossPercent > 5.0) return "#EF4444";
                if (JitterMs >= 8.0 || PacketLossPercent > 0.0 || AverageLatencyMs > 90.0) return "#F59E0B";
                return "#10B981";
            }
        }

        public string SummaryText
        {
            get
            {
                if (!IsSuccess) return "Offline / Timeout";
                return $"Avg: {AverageLatencyMs:F0}ms | Jitter: {JitterMs:F1}ms | Loss: {PacketLossPercent:F0}%";
            }
        }
    }
}
