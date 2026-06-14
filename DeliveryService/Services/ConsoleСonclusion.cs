using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DeliveryService.Tools
{
    /// <summary>
    /// Красивый консольный вывод
    /// </summary>
    public static class ConsolePretty
    {
        private static int _lastProgress = -1;

        public static void WriteHeader(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"                    {title.ToUpper()}");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.ResetColor();
        }

        public static void WriteSection(string title)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n{title}");
            Console.WriteLine("───────────────────────────────────────────────────────────────");
            Console.ResetColor();
        }

        public static void WriteInfo(string message, string emoji = "📋")
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"   {emoji} {message}");
            Console.ResetColor();
        }

        public static void WriteSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"   ✓ {message}");
            Console.ResetColor();
        }

        public static void WriteWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"   ⚠️ {message}");
            Console.ResetColor();
        }

        public static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"   ✗ {message}");
            Console.ResetColor();
        }

        public static void WriteProgress(int percent, string label = null)
        {
            if (percent == _lastProgress && percent < 100)
                return;
                
            _lastProgress = percent;
            int barWidth = 50;
            int filled = (int)(barWidth * percent / 100.0);
            string bar = new string('█', filled) + new string('░', barWidth - filled);
            
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"\r   {bar} {percent,3}%");
            if (!string.IsNullOrEmpty(label))
                Console.Write($" {label}");
            Console.ResetColor();
            
            if (percent == 100)
                Console.WriteLine();
        }

        public static void ResetProgress() => _lastProgress = -1;

        public static void WriteSeparator()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n───────────────────────────────────────────────────────────────");
            Console.ResetColor();
        }

        public static void WriteFooter(string operation = "ОПЕРАЦИЯ")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"                    ✅ {operation.ToUpper()} ЗАВЕРШЕНА");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.ResetColor();
        }

        public static void WriteStat(string key, string value)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"   ▸ {key}: {value}");
            Console.ResetColor();
        }

        public static bool AskConfirmation(string question)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"\n⚠️  {question} (y/N): ");
            Console.ResetColor();
            var response = Console.ReadLine();
            return response?.ToLower() == "y";
        }
    }
}