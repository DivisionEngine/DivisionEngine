//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using System.Runtime.CompilerServices;

namespace DivisionEngine
{
    /// <summary>
    /// Represents the severity level of a log entry.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// General info log.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Debug log (default).
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Warning log.
        /// </summary>
        /// <remarks>Can be from exception</remarks>
        Warning = 2,

        /// <summary>
        /// Error log.
        /// </summary>
        /// <remarks>Can be from exception</remarks>
        Error = 3,
    }

    /// <summary>
    /// Represents a single log entry in the debug log.
    /// </summary>
    public class LogEntry(
        string message,
        LogLevel level,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        /// <summary>
        /// Time log was created.
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.Now;

        /// <summary>
        /// Log message.
        /// </summary>
        public string Message { get; } = message;

        /// <summary>
        /// Log level.
        /// </summary>
        public LogLevel Level { get; } = level;

        /// <summary>
        /// File path where the log was called from.
        /// </summary>
        public string? FilePath { get; } = filePath;

        /// <summary>
        /// Method name where the log was called from.
        /// </summary>
        public string? MethodName { get; } = methodName;

        /// <summary>
        /// Line number where the log was called from.
        /// </summary>
        public int LineNumber { get; } = lineNumber;

        /// <summary>
        /// Full caller info string (file:line method).
        /// </summary>
        public string CallerInfo =>
            string.IsNullOrEmpty(FilePath) ? string.Empty :
            $"{System.IO.Path.GetFileName(FilePath)}:{LineNumber} {MethodName}()";

        public override string ToString() =>
            $"[{Level}] {Timestamp}: {Message} ({CallerInfo})";

        public override bool Equals(object? obj) =>
            obj is LogEntry entry && entry.Message == Message && entry.Level == Level;

        public override int GetHashCode() =>
            Message.GetHashCode() + Level.GetHashCode();
    }

    /// <summary>
    /// Debugging and logging utility for the Division Engine.
    /// </summary>
    public class Debug
    {
        private static readonly Debug instance = new Debug();
        private readonly List<LogEntry> debugLog = [];

        /// <summary>
        /// Callback invoked when a new log entry is added.
        /// </summary>
        public static event Action<LogEntry>? OnLogUpdate;

        /// <summary>
        /// Read only list of all logs stored in the debug system.
        /// </summary>
        public static IReadOnlyList<LogEntry> Logs => instance.debugLog;

        public Debug()
        {
            debugLog.Add(new LogEntry("Debug system initialized.", LogLevel.Info));
        }

        /// <summary>
        /// Creates an info log entry.
        /// </summary>
        public static void Info(string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string methodName = "",
            [CallerLineNumber] int lineNumber = 0) =>
            Log(message, LogLevel.Info, filePath, methodName, lineNumber);

        /// <summary>
        /// Creates an error log entry.
        /// </summary>
        public static void Error(string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string methodName = "",
            [CallerLineNumber] int lineNumber = 0) =>
            Log(message, LogLevel.Error, filePath, methodName, lineNumber);

        /// <summary>
        /// Creates an error log entry with exception.
        /// </summary>
        public static void Error(string message, Exception ex,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string methodName = "",
            [CallerLineNumber] int lineNumber = 0) =>
            Log(message + $" | {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}",
                LogLevel.Error, filePath, methodName, lineNumber);

        /// <summary>
        /// Creates a warning log entry.
        /// </summary>
        public static void Warning(string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string methodName = "",
            [CallerLineNumber] int lineNumber = 0) =>
            Log(message, LogLevel.Warning, filePath, methodName, lineNumber);

        /// <summary>
        /// Creates a warning log entry with exception.
        /// </summary>
        public static void Warning(string message, Exception ex,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string methodName = "",
            [CallerLineNumber] int lineNumber = 0) =>
            Log(message + $" | {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}",
                LogLevel.Warning, filePath, methodName, lineNumber);

        /// <summary>
        /// Creates a debug log entry.
        /// </summary>
        public static void Log(string message, LogLevel level = LogLevel.Debug,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string methodName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            string prefix = level switch
            {
                LogLevel.Debug => "[DEBUG]",
                LogLevel.Info => "[INFO]",
                LogLevel.Warning => "[WARNING]",
                LogLevel.Error => "[ERROR]",
                _ => "[LOG]"
            };

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                string callerInfo = string.IsNullOrEmpty(filePath) ? "" :
                    $"{Path.GetFileName(filePath)}:{lineNumber} {methodName}()";
                System.Diagnostics.Debug.WriteLine($"{prefix} {message} ({callerInfo})");
            }
#endif

            LogEntry entry = new LogEntry(message, level, filePath, methodName, lineNumber);
            Console.WriteLine(entry.ToString());
            instance.debugLog.Add(entry);
            OnLogUpdate?.Invoke(entry);
        }

        /// <summary>
        /// Clears all debug logs.
        /// </summary>
        public static void ClearLogs() => instance.debugLog.Clear();

        /// <summary>
        /// Removes a specific log entry.
        /// </summary>
        public static void ClearLog(LogEntry e) => instance.debugLog.Remove(e);
    }
}
