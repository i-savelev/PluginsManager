using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

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
            "app.log");
        private static LogLevel _level = LogLevel.Debug;

        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }

        public static void SetLogPath(string logFile)
        {
            if (string.IsNullOrWhiteSpace(logFile))
                throw new ArgumentException("logFile must be a non-empty string.", nameof(logFile));

            _logFile = Path.GetFullPath(logFile);
        }

        public static string GetLogPath()
        {
            return _logFile;
        }

        public static void SetLogLevel(LogLevel level)
        {
            _level = level;
        }

        public static void Clear()
        {
            lock (_lock)
            {
                var logDir = Path.GetDirectoryName(_logFile);
                if (!string.IsNullOrEmpty(logDir))
                    Directory.CreateDirectory(logDir);

                File.WriteAllText(_logFile, string.Empty, Encoding.UTF8);
            }
        }

        public static void Init(
            string hostName = null,
            string hostVersionNumber = null,
            string hostBuild = null,
            bool? hasActiveDocument = null)
        {
            Clear();

            var process = Process.GetCurrentProcess();
            var entryAssembly = Assembly.GetExecutingAssembly().GetName();

            Separator();
            Info("Startup environment snapshot");
            Info($"Assembly = {entryAssembly.Name} {entryAssembly.Version}");
            Info($"Process = {process.ProcessName} | PID = {process.Id} | Session = {process.SessionId}");
            Info($"Machine = {Environment.MachineName} | User = {Environment.UserDomainName}\\{Environment.UserName}");
            Info($"OS = {Environment.OSVersion} | 64-bit OS = {Environment.Is64BitOperatingSystem} | 64-bit process = {Environment.Is64BitProcess}");
            Info($".NET = {Environment.Version} | Framework = {RuntimeEnvironmentDescription()}");
            Info($"Culture = {CultureInfo.CurrentCulture.Name} | UI Culture = {CultureInfo.CurrentUICulture.Name} | TimeZone = {TimeZoneInfo.Local.DisplayName}");
            Info($"Current directory = {Environment.CurrentDirectory}");
            Info($"Command line = {Environment.CommandLine}");

            if (!string.IsNullOrWhiteSpace(hostName) ||
                !string.IsNullOrWhiteSpace(hostVersionNumber) ||
                !string.IsNullOrWhiteSpace(hostBuild))
            {
                Info($"Host = {hostName ?? "unavailable"} | VersionNumber = {hostVersionNumber ?? "unavailable"} | Build = {hostBuild ?? "unavailable"}");
            }

            if (hasActiveDocument.HasValue)
            {
                Info($"Active document available = {hasActiveDocument.Value}");
            }

            Separator();
        }

        private static string RuntimeEnvironmentDescription()
        {
            try
            {
                return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            }
            catch
            {
                return ".NET Framework";
            }
        }

        private static void WriteLog(
            LogLevel level,
            string message,
            string callerFilePath,
            int callerLineNumber)
        {
            if ((int)level < (int)_level)
                return;

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpper().PadRight(8);
            var callerInfo = $"{Path.GetFileName(callerFilePath)}:{callerLineNumber}";
            var line = $"{now} | {levelStr} | {callerInfo} | {message}";

            lock (_lock)
            {
                var logDir = Path.GetDirectoryName(_logFile);
                if (!string.IsNullOrEmpty(logDir))
                    Directory.CreateDirectory(logDir);

                using (var writer = new StreamWriter(_logFile, true, Encoding.UTF8))
                {
                    writer.WriteLine(line);
                }
            }
        }

        public static void Debug(
            string message = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) =>
            WriteLog(LogLevel.Debug, message, filePath, lineNumber);

        public static void Separator(
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) =>
            WriteLog(LogLevel.Debug, "------------------------------------------------------------------------", filePath, lineNumber);

        public static void Info(
            string message = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) =>
            WriteLog(LogLevel.Info, message, filePath, lineNumber);

        public static void Warning(
            string message = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) =>
            WriteLog(LogLevel.Warning, message, filePath, lineNumber);

        public static void Error(
            string message = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) =>
            WriteLog(LogLevel.Error, message, filePath, lineNumber);

        public static void Critical(
            string message = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) =>
            WriteLog(LogLevel.Critical, message, filePath, lineNumber);

        public static void Exception(
            Exception exception,
            string message = "",
            LogLevel level = LogLevel.Error,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (exception == null)
            {
                WriteLog(level, message, filePath, lineNumber);
                return;
            }

            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(message))
            {
                builder.Append(message);
                builder.Append(" | ");
            }

            builder.Append("Exception: ");
            builder.Append(exception.GetType().FullName);
            builder.Append(" | Message: ");
            builder.Append(exception.Message);

            if (exception.InnerException != null)
            {
                builder.Append(" | Inner: ");
                builder.Append(exception.InnerException.GetType().FullName);
                builder.Append(" - ");
                builder.Append(exception.InnerException.Message);
            }

            builder.AppendLine();
            builder.Append(exception);

            WriteLog(level, builder.ToString(), filePath, lineNumber);
        }
    }
}
