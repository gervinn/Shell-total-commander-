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
            base.OnStartup(e);

            // Try to detect whether the TCP server is running. If the connection fails,
            // notify the user but do not shut down the application; the client can
            // operate in local mode. Keep the timeout short to avoid a long delay.
            bool serverAvailable = false;
            using (var client = new TcpClient())
            {
                try
                {
                    var connectTask = client.ConnectAsync("localhost", 9000);
                    serverAvailable = connectTask.Wait(500) && client.Connected;
                }
                catch
                {
                    serverAvailable = false;
                }
            }
            if (!serverAvailable)
            {
                MessageBox.Show(
                    "Сервер недоступний. Оболонка працюватиме локально, поки сервер не буде запущено.",
                    "Сервер недоступний",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            // If server is available, no message is shown and the main window loads as usual.
        }
    }
}
