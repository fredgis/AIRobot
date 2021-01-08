using System;

namespace afpredict
{
    public class Logger
    {
        public static void LogInformation(string message)
        {
            Console.WriteLine($"{GetUtcNowFormatedDate()} - {message}");
        }

        public static void LogError(string message)
        {
            Console.Error.WriteLine($"{GetUtcNowFormatedDate()} - {message}");
        }

        private static string GetUtcNowFormatedDate()
        {
            return DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}