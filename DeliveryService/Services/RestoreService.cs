using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DeliveryService.Services
{
    public class RestoreService
    {
        private readonly string _connectionString;
        private readonly string _mediaPath;

        public RestoreService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
            _mediaPath = configuration["Backup:MediaPath"] ?? @"..\DeliveryService\Images";
        }

        public async Task RestoreFromBackupAsync(string sqlDumpPath, string mediaArchivePath = null)
        {
            if (!File.Exists(sqlDumpPath))
                throw new FileNotFoundException($"Думп не найден: {sqlDumpPath}");
            
            // 1. Восстановление БД
            await RestoreDatabaseAsync(sqlDumpPath);
            
            // 2. Восстановление медиа (если указано)
            if (!string.IsNullOrEmpty(mediaArchivePath) && File.Exists(mediaArchivePath))
            {
                RestoreMediaFiles(mediaArchivePath);
            }
        }

        private async Task RestoreDatabaseAsync(string sqlDumpPath)
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(_connectionString);
            string dbName = builder.Database;
            
            // переподключение на БД postgres для пересоздания
            builder.Database = "postgres";
            string adminConnection = builder.ConnectionString;
            
            using (var conn = new Npgsql.NpgsqlConnection(adminConnection))
            {
                await conn.OpenAsync();
                
                // убирает активные соединения
                using (var cmd = new Npgsql.NpgsqlCommand($@"
                    SELECT pg_terminate_backend(pid) 
                    FROM pg_stat_activity 
                    WHERE datname = '{dbName}' AND pid <> pg_backend_pid()", conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                
                // Пересоздание БД
                using (var cmd = new Npgsql.NpgsqlCommand($"DROP DATABASE IF EXISTS \"{dbName}\"", conn))
                    await cmd.ExecuteNonQueryAsync();
                    
                using (var cmd = new Npgsql.NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", conn))
                    await cmd.ExecuteNonQueryAsync();
            }
            
            // Восстанавление из дампа
            string psqlPath = @"C:\Program Files\PostgreSQL\15\bin\psql.exe";
            string arguments = $"-h {builder.Host} -p {builder.Port} -U {builder.Username} -d {dbName} -f \"{sqlDumpPath}\"";
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = psqlPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    Environment = { ["PGPASSWORD"] = builder.Password }
                }
            };
            
            process.Start();
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0)
                throw new Exception("Ошибка восстановления БД");
        }

        private void RestoreMediaFiles(string archivePath)
        {
            string fullMediaPath = Path.GetFullPath(_mediaPath);
            
            if (Directory.Exists(fullMediaPath))
                Directory.Delete(fullMediaPath, true);
            
            System.IO.Compression.ZipFile.ExtractToDirectory(archivePath, Path.GetDirectoryName(fullMediaPath));
        }
    }
}