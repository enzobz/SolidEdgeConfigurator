using System;
using System.IO;
using System.Windows;

namespace SolidEdgeConfigurator
{
    class Program
    {
        private static string _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "SolidEdgeConfigurator_Debug.log"
        );

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                LogMessage("=== Application Starting ===");
                LogMessage($"Current Directory: {Directory.GetCurrentDirectory()}");
                LogMessage($"Executable: {System.Reflection.Assembly.GetExecutingAssembly().Location}");

                LogMessage("Creating Application instance...");
                var app = new App();
                
                LogMessage("Creating MainWindow instance...");
                var mainWindow = new MainWindow();
                
                LogMessage("Running application...");
                app.Run(mainWindow);
                
                LogMessage("Application closed normally");
            }
            catch (Exception ex)
            {
                LogMessage($"EXCEPTION: {ex.GetType().Name}");
                LogMessage($"Message: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    LogMessage($"Inner Exception: {ex.InnerException.Message}");
                    LogMessage($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                
                LogMessage("=== Application Crashed ===");
            }
            finally
            {
                LogMessage("=== Application Ended ===");
            }
        }

        private static void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logLine = $"[{timestamp}] {message}";
            
            try
            {
                File.AppendAllText(_logPath, logLine + Environment.NewLine);
                Console.WriteLine(logLine);
            }
            catch
            {
                // Ignore file write errors
            }
        }
    }
}