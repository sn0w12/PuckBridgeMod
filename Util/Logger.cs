using System;

namespace PuckBridgeMod.Util {
    public static class Logger {
        private const string LOG_PREFIX = "[PuckBridge]";

        public enum LogLevel {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }

        public static LogLevel MinLogLevel { get; set; } = LogLevel.Info;

        public static void Debug(string message) {
            Log(LogLevel.Debug, message);
        }

        public static void Info(string message) {
            Log(LogLevel.Info, message);
        }

        public static void Warning(string message) {
            Log(LogLevel.Warning, message);
        }

        public static void Error(string message) {
            Log(LogLevel.Error, message);
        }

        public static void Error(string message, Exception exception) {
            Log(LogLevel.Error, $"{message}: {exception.Message}");
        }

        private static void Log(LogLevel level, string message) {
            if (level < MinLogLevel) return;

            string formattedMessage = $"{LOG_PREFIX} {message}";

            switch (level) {
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(formattedMessage);
                    break;
            }
        }
    }
}