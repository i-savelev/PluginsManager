using System;
using System.IO;
using System.Runtime.CompilerServices;



namespace PluginsManager
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Temp",
                "PluginsManager",
                "logs",
                "app.log"
            );
        private static LogLevel _level = LogLevel.Debug;

        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }

        public static void Configure(string logFile = "app.log", LogLevel level = LogLevel.Debug)
        {
            if (string.IsNullOrWhiteSpace(logFile))
                throw new ArgumentException("logFile must be a non-empty string.", nameof(logFile));
            _logFile = Path.GetFullPath(logFile);
            _level = level;
        }

        public static void Clear()
        {
            lock (_lock)
            {
                var logDir = Path.GetDirectoryName(_logFile);
                if (!string.IsNullOrEmpty(logDir))
                    Directory.CreateDirectory(logDir);
                File.WriteAllText(_logFile, string.Empty, System.Text.Encoding.UTF8);
            }
        }

        // Внутренний метод — БЕЗ caller-атрибутов
        private static void WriteLog(
            LogLevel level,
            string name,
            string message,
            string callerFilePath,
            int callerLineNumber)
        {
            if ((int)level < (int)_level) return;

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var levelStr = level.ToString().ToUpper().PadRight(8);
            var fileName = Path.GetFileName(callerFilePath);
            var callerInfo = $"{fileName}:{callerLineNumber}";
            var line = $"{now} | {levelStr} | {callerInfo} | {name} | {message}";

            lock (_lock)
            {
                var logDir = Path.GetDirectoryName(_logFile);
                if (!string.IsNullOrEmpty(logDir))
                    Directory.CreateDirectory(logDir);

                using (var writer = new StreamWriter(_logFile, append: true, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine(line);
                }
            }
        }

        // Публичные методы — С атрибутами вызывающего кода
        public static void Debug(
            string name = "log",
            string message = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) =>
            WriteLog(LogLevel.Debug, name, message, filePath, lineNumber);

        public static void Separator(
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) =>
            WriteLog(LogLevel.Debug, $"--------------------", "------------------------------------------------------------------------", filePath, lineNumber);

        public static void Info(
            string name = "log",
            string message = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) =>
            WriteLog(LogLevel.Info, name, message, filePath, lineNumber);

        public static void Warning(
            string name = "log",
            string message = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) =>
            WriteLog(LogLevel.Warning, name, message, filePath, lineNumber);

        public static void Error(
            string name = "log",
            string message = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) =>
            WriteLog(LogLevel.Error, name, message, filePath, lineNumber);

        public static void Critical(
            string name = "log",
            string message = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) =>
            WriteLog(LogLevel.Critical, name, message, filePath, lineNumber);
    }
}
