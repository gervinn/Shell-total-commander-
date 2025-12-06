using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace ShellTotalCommander1.Logging
{
    /// <summary>
    /// Provides simple logging to a SQLite database. All shell commands executed through the UI can be recorded
    /// along with their arguments, status and message. The log file is created in the application directory.
    /// </summary>
    public class DatabaseLogger
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseLogger"/> class. The default connection string
        /// uses a database file named <c>logs.db</c> in the current working directory.
        /// </summary>
        /// <param name="connectionString">The SQLite connection string.</param>
        public DatabaseLogger(string connectionString = "Data Source=logs.db")
        {
            _connectionString = connectionString;
            EnsureDatabase();
        }

        /// <summary>
        /// Ensures the database and the Logs table exist. This method is called automatically on construction.
        /// </summary>
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

        /// <summary>
        /// Logs the details of a command execution to the database asynchronously.
        /// </summary>
        /// <param name="command">The name of the command executed.</param>
        /// <param name="args">The arguments passed to the command.</param>
        /// <param name="success">Whether the command succeeded.</param>
        /// <param name="message">A message describing the result of the command.</param>
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