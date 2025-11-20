using System;
using ShellTotalCommander1.Server;

namespace ShellServer
{
    /// <summary>
    /// Entry point for the ShellServer console application.  This wraps the code into a Main method
    /// instead of using top‑level statements, so that it can coexist with other top‑level files in the solution.
    /// </summary>
    internal class Program
    {
        static void Main()
        {
            // Start a TCP server that listens on port 9000.
            var server = new ShellTcpServer(9000);
            server.Start();

            Console.WriteLine("Сервер запущено на порту 9000. Натисніть Enter для зупинки.");
            Console.ReadLine();

            server.Stop();
            Console.WriteLine("Сервер зупинено.");
        }
    }
}
