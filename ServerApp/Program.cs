using System;
using ShellTotalCommander1.Server;

namespace ServerApp
{
    /// <summary>
    /// Entry point for the ServerApp console application.  Wrap the code into a Main method so that this
    /// project does not use top‑level statements, preventing conflicts with other projects in the solution.
    /// </summary>
    internal class Program
    {
        static void Main()
        {
            // Launch a TCP server that listens on port 9000 and processes shell commands
            var server = new ShellTcpServer(9000);
            server.Start();

            Console.WriteLine("Сервер запущено на порту 9000. Натисніть Enter, щоб зупинити.");
            Console.ReadLine();

            server.Stop();
            Console.WriteLine("Сервер зупинено.");
        }
    }
}
