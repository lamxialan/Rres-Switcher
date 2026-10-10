using System;

namespace RRes.Models
{
    public class CleanerResult
    {
        public bool Success { get; set; }
        public long BytesCleaned { get; set; }
        public int FilesDeleted { get; set; }
        public int FilesFailed { get; set; }
        public string FormattedSize => FormatBytes(BytesCleaned);
        public string Message { get; set; } = string.Empty;

        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:F1} {sizes[order]}";
        }
    }
}
