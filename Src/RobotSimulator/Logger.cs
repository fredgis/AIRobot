using System;

namespace RobotSimulator
{
    public class Logger
    {
        public static void LogInfo(string message)
        {
            Log(message, false);
        }

        public static void LogError(string error)
        {
            Log(error, true);
        }

        private static void Log(string message, bool isError)
        {
            var timestamp = DateTimeOffset.UtcNow;
            Console.WriteLine($"{timestamp.ToString("yyyy-MM-dd HH:mm:ss")} - {message}");

            if (isError)
                Console.Error.WriteLine(message);
        }
    }
}