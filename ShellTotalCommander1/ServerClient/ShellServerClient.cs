using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShellTotalCommander1.ServerClient;

/// <summary>
/// Simple TCP client that communicates with the shell server. It serializes
/// command requests to JSON, sends them to the server, and deserializes the
/// response. This client is designed to be lightweight and stateless, creating
/// a new connection for each command.
/// </summary>
public sealed class ShellServerClient
{
    private readonly string _host;
    private readonly int _port;

    public ShellServerClient(string host = "localhost", int port = 9000)
    {
        _host = host;
        _port = port;
    }

    /// <summary>
    /// Sends a command to the server asynchronously and returns the response.
    /// </summary>
    /// <param name="command">The command name (e.g. ls, cd).</param>
    /// <param name="args">Arguments for the command.</param>
    /// <param name="currentDirectory">Current working directory.</param>
    /// <returns>A deserialized <see cref="CommandResponse"/> or null if the call fails.</returns>
    public async Task<CommandResponse?> SendCommandAsync(string command, string[] args, string? currentDirectory)
    {
        var request = new CommandRequest
        {
            Command = command,
            Args = args,
            CurrentDirectory = currentDirectory
        };
        var json = JsonSerializer.Serialize(request);

        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(_host, _port);
        }
        catch
        {
            return null;
        }
        using var stream = client.GetStream();
        var bytes = Encoding.UTF8.GetBytes(json);
        await stream.WriteAsync(bytes);
        var buffer = new byte[8192];
        int bytesRead;
        try
        {
            bytesRead = await stream.ReadAsync(buffer);
        }
        catch
        {
            return null;
        }
        if (bytesRead == 0)
        {
            return null;
        }
        var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        try
        {
            return JsonSerializer.Deserialize<CommandResponse>(responseJson);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Outgoing request to the shell server. Mirrors the server-side definition.
/// </summary>
public sealed class CommandRequest
{
    public string Command { get; set; } = string.Empty;
    public string[]? Args { get; set; }
    public string? CurrentDirectory { get; set; }
}

/// <summary>
/// Response returned from the shell server.
/// </summary>
public sealed class CommandResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Items { get; set; }
    /// <summary>
    /// The server's current working directory after executing the command. This allows
    /// the client UI to sync its local state. May be null.
    /// </summary>
    public string? CurrentDirectory { get; set; }
}