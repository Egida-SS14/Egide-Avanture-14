using System;

namespace Egide.Bot
{
    public enum LogType
    {
        CRIT = 0,
        WARN = 1,
        DEBG = 2,
        INFO = 3
    }

    public static class BotLoggerSystem
    {
        private static bool _debugMode = false;

        public static void SetDebugMode(bool debugMode)
        {
            _debugMode = debugMode;
        }

        public static void Log(LogType type, string message)
        {
            if (type == LogType.DEBG && !_debugMode)
                return;

            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine($"EgideBotLog: ({now}), [{type}] - {message}");
        }
    }
}
