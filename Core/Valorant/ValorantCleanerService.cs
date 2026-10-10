using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RRes.Models;

namespace RRes.Core.Valorant
{
    public class ValorantCleanerService
    {
        private readonly List<string> _targetDirectories = new();

        public ValorantCleanerService()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string savedDir = Path.Combine(localAppData, "VALORANT", "Saved");

            _targetDirectories.Add(Path.Combine(savedDir, "Logs"));
            _targetDirectories.Add(Path.Combine(savedDir, "Crashes"));
            _targetDirectories.Add(Path.Combine(savedDir, "webcache"));
        }

        public IReadOnlyList<string> TargetDirectories => _targetDirectories;

        /// <summary>
        /// Menghitung total ukuran file sampah & crash log di direktori target.
        /// </summary>
        public async Task<(long TotalBytes, int FileCount)> ScanJunkAsync()
        {
            return await Task.Run(() =>
            {
                long totalBytes = 0;
                int fileCount = 0;

                foreach (var dir in _targetDirectories)
                {
                    if (!Directory.Exists(dir)) continue;

                    try
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                        {
                            try
                            {
                                totalBytes += file.Length;
                                fileCount++;
                            }
                            catch
                            {
                                // Lewati jika ada file yang tidak dapat dibaca
                            }
                        }
                    }
                    catch
                    {
                        // Lewati jika direktori tidak dapat diakses
                    }
                }

                return (totalBytes, fileCount);
            });
        }

        /// <summary>
        /// Membersihkan file log, crash dump, dan webcache dengan penanganan aman try-catch.
        /// </summary>
        public async Task<CleanerResult> CleanStorageAsync()
        {
            return await Task.Run(() =>
            {
                long bytesCleaned = 0;
                int filesDeleted = 0;
                int filesFailed = 0;

                foreach (var dir in _targetDirectories)
                {
                    if (!Directory.Exists(dir)) continue;

                    try
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                        {
                            try
                            {
                                long len = file.Length;
                                file.Attributes = FileAttributes.Normal;
                                file.Delete();
                                bytesCleaned += len;
                                filesDeleted++;
                            }
                            catch
                            {
                                filesFailed++;
                            }
                        }

                        // Hapus subdirektori kosong (misal di webcache / crashes)
                        foreach (var subDir in dirInfo.EnumerateDirectories("*", SearchOption.AllDirectories))
                        {
                            try
                            {
                                if (subDir.Exists && subDir.GetFileSystemInfos().Length == 0)
                                {
                                    subDir.Delete(true);
                                }
                            }
                            catch
                            {
                                // Abaikan jika subdirektori sedang digunakan
                            }
                        }
                    }
                    catch
                    {
                        // Lewati jika direktori utama terkunci
                    }
                }

                string formatted = CleanerResult.FormatBytes(bytesCleaned);
                string message;
                if (filesDeleted > 0)
                {
                    message = $"Berhasil membersihkan {formatted} file sampah ({filesDeleted} file dihapus{(filesFailed > 0 ? $", {filesFailed} dilewati" : "")}).";
                }
                else if (filesFailed > 0)
                {
                    message = $"Ditemukan {filesFailed} file yang sedang dikunci oleh game atau sistem.";
                }
                else
                {
                    message = "Direktori log & cache Valorant sudah bersih (0 file sampah).";
                }

                return new CleanerResult
                {
                    Success = filesDeleted > 0 || (filesFailed == 0 && filesDeleted == 0),
                    BytesCleaned = bytesCleaned,
                    FilesDeleted = filesDeleted,
                    FilesFailed = filesFailed,
                    Message = message
                };
            });
        }
    }
}
