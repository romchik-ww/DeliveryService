using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using DeliveryService.Tools;

namespace DeliveryService.Services
{
    public class BackupResult
    {
        public string BackupName { get; set; }
        public DateTime Timestamp { get; set; }
        public bool DatabaseSuccess { get; set; }
        public bool MediaSuccess { get; set; }
        public string SqlDumpPath { get; set; }
        public string MediaArchivePath { get; set; }
        public long DatabaseSizeBytes { get; set; }
        public int MediaFilesCount { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsSuccess => DatabaseSuccess;
    }

    public class BackupService
    {
        private readonly string _connectionString;
        private readonly string _backupFolder;
        private readonly string _pgDumpPath;
        private readonly string _mediaFolder;

        public BackupService(string connectionString, string backupFolder, string mediaFolder, string pgDumpPath = null)
        {
            _connectionString = connectionString;
            _backupFolder = backupFolder;
            _mediaFolder = mediaFolder;
            _pgDumpPath = pgDumpPath ?? FindPgDump();
        }

        private string FindPgDump()
        {
            string[] possiblePaths = {
                @"C:\Program Files\PostgreSQL\17\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\16\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\14\bin\pg_dump.exe",
                @"tools\pg_dump.exe"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            throw new FileNotFoundException("pg_dump.exe не найден. Установите PostgreSQL.");
        }

        public async Task<BackupResult> CreateFullBackupAsync(IProgress<int> progress = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            var backupName = $"delivery_backup_{timestamp}";
            
            var sqlDumpPath = Path.Combine(_backupFolder, $"{backupName}.sql");
            var mediaArchivePath = Path.Combine(_backupFolder, $"{backupName}_media.zip");

            Directory.CreateDirectory(_backupFolder);

            ConsolePretty.WriteHeader("🔧 DELIVERYSERVICE BACKUP");
            ConsolePretty.WriteSection("📋 Информация о бэкапе");
            ConsolePretty.WriteInfo($"Имя бэкапа:    {backupName}", "🏷️");
            ConsolePretty.WriteInfo($"Папка:         {_backupFolder}", "📁");
            ConsolePretty.WriteInfo($"База данных:   {ParseDatabaseName()}", "🐘");
            ConsolePretty.WriteSeparator();

            var result = new BackupResult
            {
                BackupName = backupName,
                Timestamp = DateTime.Now
            };

            // 1. Бэкап БД
            ConsolePretty.WriteSection("💾 [1/2] Создание дампа базы данных");
            progress?.Report(10);
            
            result.DatabaseSuccess = await BackupDatabaseAsync(sqlDumpPath, progress);
            if (result.DatabaseSuccess)
            {
                var fileInfo = new FileInfo(sqlDumpPath);
                result.DatabaseSizeBytes = fileInfo.Length;
                ConsolePretty.WriteSuccess($"Дамп создан: {fileInfo.Length / 1024.0 / 1024.0:F2} MB");
            }
            else
            {
                ConsolePretty.WriteError("Ошибка создания дампа базы данных");
                return result;
            }

            progress?.Report(60);

            // 2. Бэкап медиа
            ConsolePretty.WriteSection("📦 [2/2] Архивирование медиа-файлов");
            
            if (!string.IsNullOrEmpty(_mediaFolder) && Directory.Exists(_mediaFolder))
            {
                result.MediaSuccess = await BackupMediaAsync(mediaArchivePath, progress);
                if (result.MediaSuccess)
                {
                    var fileInfo = new FileInfo(mediaArchivePath);
                    result.MediaArchivePath = mediaArchivePath;
                    var fileCount = Directory.GetFiles(_mediaFolder, "*", SearchOption.AllDirectories).Length;
                    result.MediaFilesCount = fileCount;
                    ConsolePretty.WriteSuccess($"Архив создан: {fileCount} файлов, {fileInfo.Length / 1024.0 / 1024.0:F2} MB");
                }
                else
                {
                    ConsolePretty.WriteWarning("Медиа-файлы не архивированы (папка пуста)");
                }
            }
            else
            {
                ConsolePretty.WriteWarning($"Папка {_mediaFolder} не существует, пропускаю");
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            progress?.Report(100);

            // Итог
            ConsolePretty.WriteFooter("РЕЗЕРВНОЕ КОПИРОВАНИЕ");
            ConsolePretty.WriteSection("📊 Статистика");
            ConsolePretty.WriteStat("Дамп БД", $"{result.DatabaseSizeBytes / 1024.0 / 1024.0:F2} MB");
            if (result.MediaSuccess)
                ConsolePretty.WriteStat("Медиа-архив", $"{result.MediaFilesCount} файлов");
            ConsolePretty.WriteStat("Время выполнения", $"{result.Duration.TotalSeconds:F1} сек");
            ConsolePretty.WriteSeparator();
            ConsolePretty.WriteSuccess($"Бэкап сохранён: {sqlDumpPath}");
            ConsolePretty.WriteFooter("РЕЗЕРВНОЕ КОПИРОВАНИЕ");

            return result;
        }

        private async Task<bool> BackupDatabaseAsync(string outputPath, IProgress<int> progress = null)
        {
            try
            {
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(_connectionString);
                
                var arguments = $"-h {builder.Host} -p {builder.Port} -U {builder.Username} -d {builder.Database} -F p -f \"{outputPath}\"";
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = _pgDumpPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Environment = { ["PGPASSWORD"] = builder.Password }
                };

                // Имитация прогресса (pg_dump не даёт прогресс, делаем сами)
                var progressTask = Task.Run(async () =>
                {
                    for (int i = 10; i <= 50; i += 5)
                    {
                        progress?.Report(i);
                        await Task.Delay(200);
                    }
                });

                using (var process = Process.Start(startInfo))
                {
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        ConsolePretty.WriteError($"pg_dump error: {error}");
                        return false;
                    }
                    
                    await progressTask;
                    return true;
                }
            }
            catch (Exception ex)
            {
                ConsolePretty.WriteError($"Ошибка: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> BackupMediaAsync(string archivePath, IProgress<int> progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var files = Directory.GetFiles(_mediaFolder, "*", SearchOption.AllDirectories);
                    if (files.Length == 0) return false;

                    int processed = 0;
                    using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
                    {
                        foreach (var file in files)
                        {
                            var entry = archive.CreateEntry(Path.GetRelativePath(_mediaFolder, file));
                            using (var entryStream = entry.Open())
                            using (var fileStream = File.OpenRead(file))
                            {
                                fileStream.CopyTo(entryStream);
                            }
                            processed++;
                            var percent = 50 + (int)((double)processed / files.Length * 40);
                            progress?.Report(percent);
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    ConsolePretty.WriteError($"Ошибка архивации: {ex.Message}");
                    return false;
                }
            });
        }

        private string ParseDatabaseName()
        {
            try
            {
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(_connectionString);
                return builder.Database;
            }
            catch
            {
                return "unknown";
            }
        }

        public void CleanupOldBackups(int retentionDays = 7)
        {
            var cutoff = DateTime.Now.AddDays(-retentionDays);
            var files = Directory.GetFiles(_backupFolder, "delivery_backup_*");
            int deleted = 0;
            
            foreach (var file in files)
            {
                if (File.GetCreationTime(file) < cutoff)
                {
                    File.Delete(file);
                    deleted++;
                }
            }
            
            if (deleted > 0)
                ConsolePretty.WriteInfo($"Удалено старых бэкапов: {deleted}", "🧹");
        }
    }
}