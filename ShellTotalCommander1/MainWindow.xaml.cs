using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ShellTotalCommander1.Shell;
using ShellTotalCommander1.Shell.Commands;
using ShellTotalCommander1.Logging;
using System.Threading.Tasks;
using ShellTotalCommander1.ServerClient;
using System.IO;

namespace ShellTotalCommander1;

public partial class MainWindow : Window
{
    private readonly ShellContext _shellContext = new();
    private readonly DatabaseLogger _logger = new();
    private readonly ShellTotalCommander1.ServerClient.ShellServerClient _serverClient = new();

    public MainWindow()
    {
        InitializeComponent();
        ExecuteDefaultCommand();
    }

    private void ExecuteDefaultCommand()
    {
        try
        {
            var result = _shellContext.Execute("ls");
            ApplyResult(result);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Foreground = Brushes.DarkRed;
            StatusTextBlock.Text = ex.Message;
        }
    }

    private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCurrentCommandAsync();
    }

    private void CommandTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _ = ExecuteCurrentCommandAsync();
            e.Handled = true;
        }
    }

    private async Task ExecuteCurrentCommandAsync()
    {
        var input = CommandTextBox.Text;
        if (string.IsNullOrWhiteSpace(input))
        {
            StatusTextBlock.Foreground = Brushes.DarkRed;
            StatusTextBlock.Text = "Please enter a command.";
            ItemsListView.ItemsSource = Array.Empty<object>();
            return;
        }

        try
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var commandName = parts.Length > 0 ? parts[0] : string.Empty;
            var args = parts.Skip(1).ToArray();

            // Send the command to the shell server. If it fails (null response), fallback to local execution.
            var response = await _serverClient.SendCommandAsync(commandName, args, _shellContext.CurrentDirectory.FullName);
            if (response is not null)
            {
                // When the server responds with a current working directory, update the local
                // context to mirror it. This ensures that subsequent local fallbacks stay in sync.
                if (!string.IsNullOrWhiteSpace(response.CurrentDirectory))
                {
                    try
                    {
                        _shellContext.ChangeDirectory(response.CurrentDirectory);
                    }
                    catch
                    {
                        // Ignore failures updating local context.
                    }
                }
                ApplyRemoteResult(response);
                // Log the command execution asynchronously.
                try
                {
                    await _logger.LogAsync(commandName, args, response.Success, response.Message);
                }
                catch
                {
                    // Ignore logging failures to keep UI responsive.
                }
                return;
            }

            // If server request failed, fallback to local execution and prefix the
            // response message so the user knows that the command was processed locally.
            var localResult = _shellContext.Execute(input);
            var prefixedMessage = $"[Локально] {localResult.Message}";
            // Log and update UI using the prefixed message to preserve success/failure colour.
            try
            {
                await _logger.LogAsync(commandName, args, localResult.Success, prefixedMessage);
            }
            catch
            {
                // Ignore logging failures.
            }
            // Create an adjusted result based on success or failure using the factory methods
            CommandResult adjustedResult = localResult.Success
                ? CommandResult.SuccessResult(prefixedMessage, localResult.Items)
                : CommandResult.Failure(prefixedMessage);
            ApplyResult(adjustedResult);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Foreground = Brushes.DarkRed;
            StatusTextBlock.Text = ex.Message;
            ItemsListView.ItemsSource = Array.Empty<object>();
        }
    }

    private void ApplyResult(CommandResult result)
    {
        StatusTextBlock.Foreground = result.Success ? Brushes.DarkGreen : Brushes.DarkRed;
        StatusTextBlock.Text = result.Message;
        ItemsListView.ItemsSource = result.Items.ToList();
        CommandTextBox.SelectAll();
        CommandTextBox.Focus();
    }

    /// <summary>
    /// Applies the result returned from the shell server to the UI. Converts
    /// returned file system paths into <see cref="FileItem"/> objects so that
    /// detailed information can be displayed in the list view. If no items are
    /// returned, clears the list.
    /// </summary>
    /// <param name="response">The response from the server.</param>
    private void ApplyRemoteResult(CommandResponse response)
    {
        StatusTextBlock.Foreground = response.Success ? Brushes.DarkGreen : Brushes.DarkRed;
        StatusTextBlock.Text = response.Message;
        if (response.Items is not null)
        {
            var items = response.Items.Select(path =>
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        return new FileItem(new DirectoryInfo(path));
                    }
                    if (File.Exists(path))
                    {
                        return new FileItem(new FileInfo(path));
                    }
                }
                catch
                {
                    // Ignore invalid paths
                }
                return null;
            })
            .Where(item => item is not null)!
            .ToList();
            ItemsListView.ItemsSource = items;
        }
        else
        {
            ItemsListView.ItemsSource = Array.Empty<object>();
        }
        CommandTextBox.SelectAll();
        CommandTextBox.Focus();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CommandTextBox.Focus();
    }
}
