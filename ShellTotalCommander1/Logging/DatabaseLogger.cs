using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace ShellTotalCommander1.Logging
{

    public class DatabaseLogger
    {
        private readonly string _connectionString;

        public DatabaseLogger(string connectionString = "Data Source=logs.db")
        {
            _connectionString = connectionString;
            EnsureDatabase();
        }

        private void EnsureDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Logs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    Command TEXT NOT NULL,
                    Args TEXT NOT NULL,
                    Success INTEGER NOT NULL,
                    Message TEXT
                );";
            command.ExecuteNonQuery();
        }

        public async Task LogAsync(string command, string[] args, bool success, string message)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Logs (Timestamp, Command, Args, Success, Message)
                VALUES ($ts, $command, $args, $success, $message);";
            cmd.Parameters.AddWithValue("$ts", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$command", command);
            cmd.Parameters.AddWithValue("$args", string.Join(' ', args ?? Array.Empty<string>()));
            cmd.Parameters.AddWithValue("$success", success ? 1 : 0);
            cmd.Parameters.AddWithValue("$message", message ?? string.Empty);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}