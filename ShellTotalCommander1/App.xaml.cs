using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace ShellTotalCommander1
{

    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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
        }
    }
}
