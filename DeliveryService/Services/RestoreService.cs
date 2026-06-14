using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Npgsql;
using DeliveryService.Tools;

namespace DeliveryService.Services
{
    public class RestoreService
    {
        private readonly string _connectionString;
        private readonly string _mediaFolder;
        private readonly string _psqlPath;

        public RestoreService(string connectionString, string mediaFolder, string psqlPath = null)
        {
            _connectionString = connectionString;
            _mediaFolder = mediaFolder;
            _psqlPath = psqlPath ?? FindPsql();
        }

        private string FindPsql()
        {
            string[] possiblePaths = {
                @"C:\Program Files\PostgreSQL\17\bin\psql.exe",
                @"C:\Program Files\PostgreSQL\16\bin\psql.exe",
                @"C:\Program Files\PostgreSQL\15\bin\psql.exe",
                @"tools\psql.exe"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            throw new FileNotFoundException("psql.exe не найден");
        }

        public async Task<bool> RestoreFullBackupAsync(string sqlDumpPath, string mediaArchivePath = null, IProgress<int> progress = null)
        {
            if (!File.Exists(sqlDumpPath))
            {
                ConsolePretty.WriteError($"Файл дампа не найден: {sqlDumpPath}");
                return false;
            }

            var fileInfo = new FileInfo(sqlDumpPath);
            ConsolePretty.WriteHeader("🔧 DELIVERYSERVICE RESTORE");
            ConsolePretty.WriteSection("📋 Информация о бэкапе");
            ConsolePretty.WriteInfo($"Файл дампа:    {Path.GetFileName(sqlDumpPath)}", "📄");
            ConsolePretty.WriteInfo($"Размер:        {fileInfo.Length / 1024.0 / 1024.0:F2} MB", "📏");
            ConsolePretty.WriteInfo($"Создан:        {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}", "🕐");
            
            if (!string.IsNullOrEmpty(mediaArchivePath) && File.Exists(mediaArchivePath))
            {
                var mediaInfo = new FileInfo(mediaArchivePath);
                ConsolePretty.WriteInfo($"Архив медиа:   {Path.GetFileName(mediaArchivePath)}", "📦");
                ConsolePretty.WriteInfo($"Размер архива: {mediaInfo.Length / 1024.0 / 1024.0:F2} MB", "📏");
            }

            ConsolePretty.WriteSeparator();

            // Подтверждение
            ConsolePretty.WriteWarning("ВНИМАНИЕ! Восстановление полностью затрёт:");
            ConsolePretty.WriteInfo($"Базу данных '{ParseDatabaseName()}'", "▸");
            if (Directory.Exists(_mediaFolder))
                ConsolePretty.WriteInfo($"Папку '{Path.GetFileName(_mediaFolder)}'", "▸");

            if (!ConsolePretty.AskConfirmation("Продолжить восстановление?"))
            {
                ConsolePretty.WriteError("Операция отменена");
                return false;
            }

            var stopwatch = Stopwatch.StartNew();
            progress?.Report(5);

            // 1. Очистка БД
            ConsolePretty.WriteSection("🗑️  [1/3] Очистка текущей базы данных");
            
            if (await ClearDatabaseAsync())
            {
                ConsolePretty.WriteSuccess("Подключение к PostgreSQL установлено");
                ConsolePretty.WriteSuccess("Активные соединения разорваны");
                ConsolePretty.WriteSuccess($"База данных '{ParseDatabaseName()}' удалена");
                ConsolePretty.WriteSuccess($"База данных '{ParseDatabaseName()}' создана заново");
            }
            else
            {
                ConsolePretty.WriteError("Ошибка очистки базы данных");
                return false;
            }

            progress?.Report(30);

            // 2. Восстановление БД
            ConsolePretty.WriteSection("🔄 [2/3] Восстановление данных из дампа");
            
            var recordsCount = await RestoreDatabaseAsync(sqlDumpPath, progress);
            if (recordsCount >= 0)
            {
                ConsolePretty.WriteSuccess($"Таблицы восстановлены (всего записей: {recordsCount})");
            }
            else
            {
                ConsolePretty.WriteError("Ошибка восстановления базы данных");
                return false;
            }

            progress?.Report(70);

            // 3. Восстановление медиа
            if (!string.IsNullOrEmpty(mediaArchivePath) && File.Exists(mediaArchivePath))
            {
                ConsolePretty.WriteSection("📂 [3/3] Восстановление медиа-файлов");
                
                var filesCount = await RestoreMediaAsync(mediaArchivePath, progress);
                ConsolePretty.WriteSuccess($"Восстановлено файлов: {filesCount}");
            }

            stopwatch.Stop();
            progress?.Report(100);

            // Итог
            ConsolePretty.WriteFooter("ВОССТАНОВЛЕНИЕ");
            ConsolePretty.WriteSection("📊 Статистика");
            ConsolePretty.WriteStat("База данных", $"{ParseDatabaseName()} ({recordsCount} записей)");
            ConsolePretty.WriteStat("Время работы", $"{stopwatch.Elapsed.TotalSeconds:F1} сек");
            ConsolePretty.WriteSeparator();
            ConsolePretty.WriteSuccess("🚀 Приложение готово к работе!");
            ConsolePretty.WriteFooter("ВОССТАНОВЛЕНИЕ");

            return true;
        }

        private async Task<bool> ClearDatabaseAsync()
        {
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(_connectionString);
                string dbName = builder.Database;
                
                builder.Database = "postgres";
                string adminConnection = builder.ConnectionString;

                using (var conn = new NpgsqlConnection(adminConnection))
                {
                    await conn.OpenAsync();
                    
                    // Разрываем соединения
                    using (var cmd = new Npgsql.NpgsqlCommand($@"
                        SELECT pg_terminate_backend(pid) 
                        FROM pg_stat_activity 
                        WHERE datname = '{dbName}' AND pid <> pg_backend_pid()", conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    
                    // Пересоздаём БД
                    using (var cmd = new Npgsql.NpgsqlCommand($"DROP DATABASE IF EXISTS \"{dbName}\"", conn))
                        await cmd.ExecuteNonQueryAsync();
                        
                    using (var cmd = new Npgsql.NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", conn))
                        await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                ConsolePretty.WriteError($"Ошибка очистки: {ex.Message}");
                return false;
            }
        }

        private async Task<int> RestoreDatabaseAsync(string sqlDumpPath, IProgress<int> progress = null)
        {
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(_connectionString);
                string dbName = builder.Database;
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = _psqlPath,
                    Arguments = $"-h {builder.Host} -p {builder.Port} -U {builder.Username} -d {dbName} -f \"{sqlDumpPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Environment = { ["PGPASSWORD"] = builder.Password }
                };

                using (var process = Process.Start(startInfo))
                {
                    // Читаем вывод, чтобы подсчитать строки
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        ConsolePretty.WriteError($"psql error: {error}");
                        return -1;
                    }
                    
                    // Примерный подсчёт записей (по числу INSERT)
                    var lines = output.Split('\n');
                    int records = 0;
                    foreach (var line in lines)
                    {
                        if (line.Contains("INSERT 0 1"))
                            records++;
                    }
                    
                    for (int i = 30; i <= 60; i += 5)
                        progress?.Report(i);
                    
                    return records;
                }
            }
            catch (Exception ex)
            {
                ConsolePretty.WriteError($"Ошибка восстановления: {ex.Message}");
                return -1;
            }
        }

        private async Task<int> RestoreMediaAsync(string archivePath, IProgress<int> progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (Directory.Exists(_mediaFolder))
                        Directory.Delete(_mediaFolder, true);
                    
                    Directory.CreateDirectory(Directory.GetParent(_mediaFolder).FullName);
                    ZipFile.ExtractToDirectory(archivePath, Directory.GetParent(_mediaFolder).FullName);
                    
                    var filesCount = Directory.GetFiles(_mediaFolder, "*", SearchOption.AllDirectories).Length;
                    
                    for (int i = 70; i <= 95; i += 5)
                        progress?.Report(i);
                    
                    return filesCount;
                }
                catch (Exception ex)
                {
                    ConsolePretty.WriteError($"Ошибка распаковки: {ex.Message}");
                    return 0;
                }
            });
        }

        private string ParseDatabaseName()
        {
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(_connectionString);
                return builder.Database;
            }
            catch
            {
                return "delivery_db";
            }
        }
    }
}