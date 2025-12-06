using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ShellTotalCommander1.Shell;
using System.Collections.Generic;

namespace ShellTotalCommander1.Server
{
    /// <summary>
    /// Represents a simple TCP server that accepts JSON-formatted requests,
    /// executes shell commands using the <see cref="ShellContext"/> and returns JSON responses.
    /// The server expects a <see cref="CommandRequest"/> object in JSON and responds with a <see cref="CommandResponse"/>.
    /// </summary>
    public class ShellTcpServer
    {
        private readonly int _port;
        private TcpListener? _listener;
        private bool _running;

        public ShellTcpServer(int port)
        {
            _port = port;
        }

        /// <summary>
        /// Starts the TCP server and begins accepting client connections.
        /// </summary>
        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _running = true;
            _ = AcceptClientsAsync();
        }

        /// <summary>
        /// Stops the TCP server and closes any active listener.
        /// </summary>
        public void Stop()
        {
            _running = false;
            _listener?.Stop();
        }

        private async Task AcceptClientsAsync()
        {
            while (_running)
            {
                var client = await _listener!.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using var stream = client.GetStream();
            var context = new ShellContext();
            var buffer = new byte[8192];
            while (_running && client.Connected)
            {
                int bytesRead = 0;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                {
                    break;
                }

                var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                try
                {
                    var request = JsonSerializer.Deserialize<CommandRequest>(json);
                    if (request != null)
                    {
                        // Change directory if provided and exists
                        if (!string.IsNullOrWhiteSpace(request.CurrentDirectory) &&
                            Directory.Exists(request.CurrentDirectory))
                        {
                            context.ChangeDirectory(request.CurrentDirectory);
                        }

                        var input = request.Command + " " + string.Join(' ', request.Args ?? Array.Empty<string>());
                        var result = context.Execute(input);
                        var response = new CommandResponse
                        {
                            Success = result.Success,
                            Message = result.Message,
                            Items = result.Items.Select(i => i.FullPath).ToList(),
                            // Provide the current working directory so the client can update
                            CurrentDirectory = context.CurrentDirectory.FullName
                        };
                        var respJson = JsonSerializer.Serialize(response);
                        var respBytes = Encoding.UTF8.GetBytes(respJson);
                        await stream.WriteAsync(respBytes);
                    }
                }
                catch (Exception ex)
                {
                    var resp = new CommandResponse
                    {
                        Success = false,
                        Message = ex.Message,
                        Items = null,
                        CurrentDirectory = context.CurrentDirectory.FullName
                    };
                    var respJson = JsonSerializer.Serialize(resp);
                    var respBytes = Encoding.UTF8.GetBytes(respJson);
                    await stream.WriteAsync(respBytes);
                }
            }
        }
    }

    /// <summary>
    /// A representation of the incoming request from a TCP client.
    /// </summary>
    public class CommandRequest
    {
        public string Command { get; set; } = string.Empty;
        public string[]? Args { get; set; }
        public string? CurrentDirectory { get; set; }
    }

    /// <summary>
    /// A representation of the outgoing response to a TCP client.
    /// </summary>
    public class CommandResponse
    {
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Items { get; set; }
    /// <summary>
    /// The server's current working directory after executing the command. This
    /// allows the client to synchronize its local context with the server. If
    /// the directory is unchanged or unknown, this value may be null.
    /// </summary>
    public string? CurrentDirectory { get; set; }
    }
}