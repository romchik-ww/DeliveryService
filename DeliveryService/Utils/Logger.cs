using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DeliveryService.Utils
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logDirectory = "logs";
        private static bool _consoleOutput = true;
        private static bool _fileOutput = true;
        
        // Настройки ротации логов
        private static long _maxFileSizeBytes = 10 * 1024 * 1024; // 10 MB по умолчанию
        private static int _maxArchiveFiles = 5; // Максимум 5 архивных файлов
        
        /// <summary>
        /// Настройка логгера
        /// </summary>
        /// <param name="logDirectory">Директория для логов</param>
        /// <param name="enableConsole">Включить вывод в консоль</param>
        /// <param name="enableFile">Включить запись в файл</param>
        /// <param name="maxFileSizeMB">Максимальный размер файла в МБ (по умолчанию 10)</param>
        /// <param name="maxArchiveFiles">Максимальное количество архивных файлов (по умолчанию 5)</param>
        public static void Configure(string logDirectory = "logs", 
                                     bool enableConsole = true, 
                                     bool enableFile = true,
                                     int maxFileSizeMB = 10,
                                     int maxArchiveFiles = 5)
        {
            _logDirectory = logDirectory;
            _consoleOutput = enableConsole;
            _fileOutput = enableFile;
            _maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;
            _maxArchiveFiles = maxArchiveFiles;
            
            if (_fileOutput && !Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
                if (_consoleOutput)
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:46] - Создана директория для логов: {Path.GetFullPath(_logDirectory)}");
            }
            
            if (_consoleOutput)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:51] - Логгер настроен: MaxFileSize={maxFileSizeMB}MB, MaxArchiveFiles={maxArchiveFiles}");
            }
        }
        
        public static void LogDebug(string message, 
                                   [CallerMemberName] string member = "", 
                                   [CallerFilePath] string file = "",
                                   [CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Debug, message, member, file, line);
        }
        
        public static void LogInfo(string message, 
                                  [CallerMemberName] string member = "", 
                                  [CallerFilePath] string file = "",
                                  [CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Info, message, member, file, line);
        }
        
        public static void LogWarning(string message, 
                                     [CallerMemberName] string member = "", 
                                     [CallerFilePath] string file = "",
                                     [CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Warning, message, member, file, line);
        }
        
        public static void LogError(string message, 
                                   Exception? ex = null, 
                                   [CallerMemberName] string member = "", 
                                   [CallerFilePath] string file = "",
                                   [CallerLineNumber] int line = 0)
        {
            var errorMessage = message;
            if (ex != null)
            {
                errorMessage = $"{message} | Exception: {ex.GetType().Name}: {ex.Message} | StackTrace: {ex.StackTrace?.Replace(Environment.NewLine, " ")}";
            }
            Log(LogLevel.Error, errorMessage, member, file, line);
        }
        
        /// <summary>
        /// Формат: [Дата и время] [Уровень] [Имя файла/модуля:Строка кода] - Сообщение
        /// </summary>
        private static void Log(LogLevel level, string message, string member, string file, int line)
        {
            // Получает только имя файла без пути
            string fileName = Path.GetFileName(file);
            
            // Формирует строку лога в строгом соответствии с требуемым форматом
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level.ToString().ToUpper()}] [{fileName}:{line}] - {message}";
            
            // Вывод в консоль с цветом
            if (_consoleOutput)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = level switch
                {
                    LogLevel.Error => ConsoleColor.Red,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Info => ConsoleColor.Green,
                    LogLevel.Debug => ConsoleColor.Cyan,
                    _ => ConsoleColor.Gray
                };
                Console.WriteLine(logEntry);
                Console.ForegroundColor = originalColor;
            }
            
            // Запись в файл
            if (_fileOutput)
            {
                lock (_lock)
                {
                    try
                    {
                        var currentLogFile = Path.Combine(_logDirectory, $"app_{DateTime.Now:yyyy-MM-dd}.log");
                        
                        // Проверяет размер файла перед записью
                        CheckAndRotateLogFile(currentLogFile);
                        
                        // Записывает новую запись
                        File.AppendAllText(currentLogFile, logEntry + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка записи в лог-файл: {ex.Message}");
                        if (_consoleOutput)
                            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] [Logger.cs:{(ex.StackTrace?.Split(':').LastOrDefault() ?? "0")}] - Не удалось записать в файл: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Проверяет размер файла и выполняет ротацию при необходимости
        /// </summary>
        private static void CheckAndRotateLogFile(string logFilePath)
        {
            if (!File.Exists(logFilePath))
                return;
            
            var fileInfo = new FileInfo(logFilePath);
            
            // Если файл превышает максимальный размер
            if (fileInfo.Length >= _maxFileSizeBytes)
            {
                RotateLogFile(logFilePath);
            }
        }
        
        /// <summary>
        /// Выполняет ротацию лог-файлов
        /// </summary>
        private static void RotateLogFile(string logFilePath)
        {
            try
            {
                // Получает базовое имя файла без расширения
                string baseFileName = Path.GetFileNameWithoutExtension(logFilePath);
                string directory = Path.GetDirectoryName(logFilePath) ?? _logDirectory;
                string extension = Path.GetExtension(logFilePath);
                
                // Ищем все существующие архивные файлы для этого дня
                var existingArchives = Directory.GetFiles(directory, $"{baseFileName}_*.log*")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.Name)
                    .ToList();
                
                // Удаляет старые архивы, если их больше максимального количества
                while (existingArchives.Count >= _maxArchiveFiles)
                {
                    var oldestArchive = existingArchives.Last();
                    oldestArchive.Delete();
                    existingArchives.Remove(oldestArchive);
                    
                    if (_consoleOutput)
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - Удален старый архив: {oldestArchive.Name}");
                }
                
                // Создание нового имени для архива
                int archiveNumber = existingArchives.Count + 1;
                string archiveFileName = $"{baseFileName}_{archiveNumber}{extension}";
                string archivePath = Path.Combine(directory, archiveFileName);
                
                // Перемещение текущего файла в архив
                File.Move(logFilePath, archivePath);
                
                if (_consoleOutput)
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - Выполнена ротация логов: {Path.GetFileName(logFilePath)} -> {archiveFileName}");
            }
            catch (Exception ex)
            {
                if (_consoleOutput)
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - Ошибка при ротации логов: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Очищает все логи (удаляет папку с логами)
        /// </summary>
        public static void ClearLogs()
        {
            lock (_lock)
            {
                try
                {
                    if (Directory.Exists(_logDirectory))
                    {
                        Directory.Delete(_logDirectory, true);
                        Directory.CreateDirectory(_logDirectory);
                        
                        if (_consoleOutput)
                            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - Все логи очищены");
                    }
                }
                catch (Exception ex)
                {
                    if (_consoleOutput)
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - Ошибка при очистке логов: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Получает информацию о текущих лог-файлах
        /// </summary>
        public static void ShowLogInfo()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - Лог-файлы отсутствуют");
                return;
            }
            
            var logFiles = Directory.GetFiles(_logDirectory, "*.log*")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();
            
            Console.WriteLine($"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - === ИНФОРМАЦИЯ О ЛОГАХ ===");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - Директория: {Path.GetFullPath(_logDirectory)}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - Макс. размер файла: {_maxFileSizeBytes / 1024 / 1024} MB");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - Макс. архивов: {_maxArchiveFiles}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - Всего файлов: {logFiles.Count}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - Общий размер: {logFiles.Sum(f => f.Length) / 1024 / 1024} MB");
            Console.WriteLine($"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - Текущие файлы:");
            
            foreach (var file in logFiles)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] -   - {file.Name} ({file.Length / 1024} KB) [{file.LastWriteTime:yyyy-MM-dd HH:mm:ss}]");
            }
            Console.WriteLine($"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [Logger.cs:{(new System.Diagnostics.StackTrace().GetFrame(0)?.GetFileLineNumber() ?? 0)}] - ========================\n");
        }
    }
    
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}