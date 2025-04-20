using System;
using System.IO;
using System.Text;

namespace MidiVolumeMixer.Utils
{
    public static class Logger
    {
        private const string LOG_FILE = "settings_debug.log";
        private static readonly string LogFilePath;

        static Logger()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataFolder, "MidiVolumeMixer");
            
            // Create the directory if it doesn't exist
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            LogFilePath = Path.Combine(appFolder, LOG_FILE);

            // Clear the log file at startup
            try
            {
                File.WriteAllText(LogFilePath, $"--- Log started at {DateTime.Now} ---\r\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing log file: {ex.Message}");
            }
        }

        public static void Log(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n";
                File.AppendAllText(LogFilePath, logEntry);
                Console.WriteLine(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log: {ex.Message}");
            }
        }

        public static void LogException(string context, Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] EXCEPTION in {context}:");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"StackTrace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                sb.AppendLine($"InnerException: {ex.InnerException.Message}");
            }
            
            try
            {
                File.AppendAllText(LogFilePath, sb.ToString());
                Console.WriteLine(sb.ToString());
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Error writing exception to log: {logEx.Message}");
            }
        }
    }
}