using System.Windows;

namespace SolidEdgeConfigurator
{
    public partial class App : Application
    {
        // Override StartupUri - we'll handle window creation manually
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Don't auto-create main window - Program.cs will do it
        }
    }
}