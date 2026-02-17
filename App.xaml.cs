using System.Windows;
using Serilog;

namespace SolidEdgeConfigurator
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure Serilog once at application startup
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("SolidEdgeConfigurator application started");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("SolidEdgeConfigurator application exiting");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}