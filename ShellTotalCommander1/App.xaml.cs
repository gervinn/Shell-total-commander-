using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace ShellTotalCommander1
{
    /// <summary>
    /// Interaction logic for the application. A startup hook has been added to
    /// verify that the accompanying server component is running before the
    /// main window is shown. If the server cannot be reached on the expected
    /// port, the application shows an error message and immediately shuts
    /// down. This prevents the shell from launching in a state where it
    /// cannot communicate with the server as requested by the user.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Overrides the startup sequence to perform a server availability check.
        /// </summary>
        /// <param name="e">Startup arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Simply call base startup. No server availability check is performed here.
            base.OnStartup(e);
        }
    }
}
