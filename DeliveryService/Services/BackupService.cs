using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DeliveryService.Services
{
    public class BackupService
    {
        private readonly string _connectionString;
        private readonly string _backupPath;
        private readonly string _mediaPath;

        public BackupService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
            _backupPath = configuration["Backup:Path"] ?? @"C:\Backups\DeliveryService";
            _mediaPath = configuration["Backup:MediaPath"] ?? @"..\DeliveryService\Images";
            
            Directory.CreateDirectory(_backupPath);
        }

        public async Task CreateBackupAsync()
        {
            string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            string backupName = $"delivery_backup_{timestamp}";
            
            // 1. Бэкап БД
            await BackupDatabaseAsync(backupName);
            
            // 2. Бэкап медиа-файлов
            await BackupMediaFilesAsync(backupName);
            
            // 3. Очистка старых бэкапов
            CleanupOldBackups();
        }

        private async Task BackupDatabaseAsync(string backupName)
        {
            string dumpFile = Path.Combine(_backupPath, $"{backupName}.sql");
            
            // парсит connection string
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(_connectionString);
            
            string pgDumpPath = @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe";
            string arguments = $"-h {builder.Host} -p {builder.Port} -U {builder.Username} -d {builder.Database} -F p -f \"{dumpFile}\"";
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pgDumpPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    Environment = { ["PGPASSWORD"] = builder.Password }
                }
            };
            
            process.Start();
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0)
                throw new Exception("Ошибка создания дампа БД");
        }

        private async Task BackupMediaFilesAsync(string backupName)
        {
            string mediaArchive = Path.Combine(_backupPath, $"{backupName}_media.zip");
            string fullMediaPath = Path.GetFullPath(_mediaPath);
            
            if (Directory.Exists(fullMediaPath))
            {
                System.IO.Compression.ZipFile.CreateFromDirectory(fullMediaPath, mediaArchive);
            }
        }

        private void CleanupOldBackups()
        {
            var files = Directory.GetFiles(_backupPath, "delivery_backup_*");
            var cutoff = DateTime.Now.AddDays(-7);
            
            foreach (var file in files)
            {
                if (File.GetCreationTime(file) < cutoff)
                    File.Delete(file);
            }
        }
    }
}