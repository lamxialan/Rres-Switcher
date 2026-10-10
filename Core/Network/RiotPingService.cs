using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using RRes.Models;

namespace RRes.Core.Network
{
    public class ServerPingResult
    {
        public string ServerName { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public long LatencyMs { get; set; }
        public bool IsSuccess { get; set; }

        public string StatusColor => !IsSuccess ? "#EF4444" : LatencyMs < 50 ? "#10B981" : LatencyMs < 100 ? "#F59E0B" : "#EF4444";
        public string DisplayText => IsSuccess ? $"{ServerName}: {LatencyMs} ms" : $"{ServerName}: Offline";
    }

    public class RiotPingService
    {
        // Server pods terdekat untuk pemain Valorant Asia Tenggara (AWS & Riot direct peering)
        public const string SgHost = "dynamodb.ap-southeast-1.amazonaws.com"; // AWS Singapore AP
        public const string TokyoHost = "dynamodb.ap-northeast-1.amazonaws.com"; // AWS Tokyo JP
        public const string HkHost = "dynamodb.ap-east-1.amazonaws.com"; // AWS Hong Kong

        /// <summary>
        /// Pengukuran cepat latensi single-shot untuk update status bar / badge.
        /// </summary>
        public async Task<ServerPingResult> CheckSingaporeLatencyAsync(int timeoutMs = 2500)
        {
            return await MeasureSingleLatencyAsync("SG Server", SgHost, timeoutMs);
        }

        public async Task<ServerPingResult> CheckTokyoLatencyAsync(int timeoutMs = 2500)
        {
            return await MeasureSingleLatencyAsync("Tokyo", TokyoHost, timeoutMs);
        }

        public async Task<ServerPingResult> CheckHongKongLatencyAsync(int timeoutMs = 2500)
        {
            return await MeasureSingleLatencyAsync("HK Server", HkHost, timeoutMs);
        }

        /// <summary>
        /// Menjalankan uji diagnostik jaringan batch lengkap (10-15 paket) untuk mengukur Latensi Rata-rata, Jitter, dan Packet Loss.
        /// </summary>
        public async Task<NetworkQualityResult> RunBatchQualityTestAsync(
            string serverName, 
            string host, 
            int packetCount = 10, 
            int timeoutMs = 1200, 
            int delayBetweenPacketsMs = 50)
        {
            return await Task.Run(async () =>
            {
                var rtts = new List<double>();
                int sent = packetCount;
                int received = 0;

                for (int i = 0; i < packetCount; i++)
                {
                    double? rtt = await ProbeSinglePacketAsync(host, timeoutMs);
                    if (rtt.HasValue && rtt.Value >= 0)
                    {
                        rtts.Add(rtt.Value);
                        received++;
                    }

                    if (i < packetCount - 1 && delayBetweenPacketsMs > 0)
                    {
                        await Task.Delay(delayBetweenPacketsMs);
                    }
                }

                double packetLoss = sent > 0 ? ((sent - received) / (double)sent) * 100.0 : 100.0;
                double avgLatency = rtts.Count > 0 ? rtts.Average() : 0;
                double minLatency = rtts.Count > 0 ? rtts.Min() : 0;
                double maxLatency = rtts.Count > 0 ? rtts.Max() : 0;

                // Hitung Jitter (Mean Absolute Difference antar paket berurutan)
                double jitter = 0;
                if (rtts.Count > 1)
                {
                    double sumDiff = 0;
                    for (int j = 1; j < rtts.Count; j++)
                    {
                        sumDiff += Math.Abs(rtts[j] - rtts[j - 1]);
                    }
                    jitter = sumDiff / (rtts.Count - 1);
                }

                return new NetworkQualityResult
                {
                    ServerName = serverName,
                    Host = host,
                    PacketsSent = sent,
                    PacketsReceived = received,
                    PacketLossPercent = Math.Round(packetLoss, 1),
                    AverageLatencyMs = Math.Round(avgLatency, 1),
                    MinLatencyMs = Math.Round(minLatency, 1),
                    MaxLatencyMs = Math.Round(maxLatency, 1),
                    JitterMs = Math.Round(jitter, 1)
                };
            });
        }

        private static async Task<double?> ProbeSinglePacketAsync(string host, int timeoutMs)
        {
            // 1. Coba ICMP Ping
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, timeoutMs);
                if (reply.Status == IPStatus.Success && reply.RoundtripTime >= 0)
                {
                    return reply.RoundtripTime;
                }
            }
            catch
            {
                // Fallback ke TCP Socket
            }

            // 2. Fallback: TCP Handshake Connect (Port 443)
            try
            {
                var sw = Stopwatch.StartNew();
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, 443);
                var completed = await Task.WhenAny(connectTask, Task.Delay(timeoutMs));

                if (completed == connectTask && client.Connected)
                {
                    sw.Stop();
                    return sw.Elapsed.TotalMilliseconds;
                }
            }
            catch
            {
                // Timeout / Gagal
            }

            return null;
        }

        private static async Task<ServerPingResult> MeasureSingleLatencyAsync(string serverName, string host, int timeoutMs)
        {
            var rtt = await ProbeSinglePacketAsync(host, timeoutMs);
            if (rtt.HasValue)
            {
                return new ServerPingResult
                {
                    ServerName = serverName,
                    Host = host,
                    LatencyMs = (long)Math.Round(rtt.Value),
                    IsSuccess = true
                };
            }

            return new ServerPingResult
            {
                ServerName = serverName,
                Host = host,
                LatencyMs = -1,
                IsSuccess = false
            };
        }
    }
}
