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
using System.Collections.Generic;
using System.Diagnostics;

namespace ShellTotalCommander1;

public partial class MainWindow : Window
{
    private readonly ShellContext _shellContext = new();
    private readonly DatabaseLogger _logger = new();
    private readonly ShellTotalCommander1.ServerClient.ShellServerClient _serverClient = new();

    // Indicates that previous attempts to connect to the server failed. When true,
    // the client will temporarily skip remote calls until the retry time has passed.
    private bool _serverUnavailable;
    private DateTime _serverRetryUntil;

    // History stack for navigating back to previously visited directories.
    private readonly Stack<string> _backHistory = new();

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

            // Determine whether to attempt a remote call. If the server was unavailable recently
            // and the retry window has not expired, skip the remote attempt.
            CommandResponse? response = null;
            bool attemptedRemote = false;
            if (!_serverUnavailable || DateTime.Now >= _serverRetryUntil)
            {
                attemptedRemote = true;
                response = await _serverClient.SendCommandAsync(commandName, args, _shellContext.CurrentDirectory.FullName);
            }
            if (response is not null)
            {
                // Reset unavailability state when the server responds.
                _serverUnavailable = false;
                _serverRetryUntil = default;
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
                // Fire-and-forget logging. Don't await to avoid blocking the UI thread.
                try
                {
                    _ = _logger.LogAsync(commandName, args, response.Success, response.Message);
                }
                catch
                {
                    // Ignore logging failures to keep UI responsive.
                }
                return;
            }
            // If remote was attempted and failed, mark the server as unavailable and set a retry window.
            if (attemptedRemote)
            {
                _serverUnavailable = true;
                _serverRetryUntil = DateTime.Now.AddSeconds(5);
            }

            // If server request failed, fallback to local execution and prefix the
            // response message so the user knows that the command was processed locally.
            var localResult = _shellContext.Execute(input);
            var prefixedMessage = $"[Локально] {localResult.Message}";
            // Log asynchronously in the background without awaiting. This prevents I/O from delaying the UI.
            try
            {
                _ = _logger.LogAsync(commandName, args, localResult.Success, prefixedMessage);
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
        UpdateBreadcrumbs();
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
        // Prefix the status message to indicate that the result came from the server
        var displayMessage = $"[Сервер] {response.Message}";
        StatusTextBlock.Text = displayMessage;
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
        UpdateBreadcrumbs();
        CommandTextBox.SelectAll();
        CommandTextBox.Focus();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CommandTextBox.Focus();
    }

    /// <summary>
    /// Handles double-clicking on an item in the list view. If the item is a directory,
    /// navigates into it by issuing a cd command and then automatically executes
    /// an ls command to refresh the listing. If the item is a file, attempts to
    /// open it with the default associated application.
    /// </summary>
    private async void ItemsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Avoid action if nothing is selected
        if (ItemsListView.SelectedItem is not FileItem selected)
        {
            return;
        }
        // If it's a directory, navigate into it
        if (selected.IsDirectory)
        {
            // Store the current directory for back navigation
            try
            {
                _backHistory.Push(_shellContext.CurrentDirectory.FullName);
            }
            catch
            {
                // Ignore failures retrieving current directory
            }
            // Issue a cd command to navigate into the selected directory
            CommandTextBox.Text = $"cd \"{selected.FullPath}\"";
            await ExecuteCurrentCommandAsync();
            // After navigating, automatically list contents
            CommandTextBox.Text = "ls";
            await ExecuteCurrentCommandAsync();
        }
        else
        {
            // Try to open the file using the default program
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = selected.FullPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Foreground = Brushes.DarkRed;
                StatusTextBlock.Text = $"Не вдалося відкрити файл: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Navigates up to the parent directory. Pushes the current directory onto the
    /// history stack and issues a cd .. command. Afterwards executes ls to refresh.
    /// </summary>
    private async void UpButton_Click(object sender, RoutedEventArgs e)
    {
        // Record current directory for back navigation
        try
        {
            _backHistory.Push(_shellContext.CurrentDirectory.FullName);
        }
        catch
        {
            // Ignore failures retrieving current directory
        }
        CommandTextBox.Text = "cd ..";
        await ExecuteCurrentCommandAsync();
        CommandTextBox.Text = "ls";
        await ExecuteCurrentCommandAsync();
    }

    /// <summary>
    /// Goes back to the previously visited directory if available. Pops from the history
    /// and navigates to it, then executes ls.
    /// </summary>
    private async void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_backHistory.Count == 0)
        {
            StatusTextBlock.Foreground = Brushes.DarkRed;
            StatusTextBlock.Text = "Немає попередньої директорії.";
            return;
        }
        var previous = _backHistory.Pop();
        CommandTextBox.Text = $"cd \"{previous}\"";
        await ExecuteCurrentCommandAsync();
        CommandTextBox.Text = "ls";
        await ExecuteCurrentCommandAsync();
    }

    /// <summary>
    /// Updates the breadcrumb navigation panel based on the current directory.
    /// Each segment of the path is rendered as a clickable button. Clicking a segment
    /// navigates to that directory and refreshes the view.
    /// </summary>
    private void UpdateBreadcrumbs()
    {
        if (PathPanel == null)
        {
            return;
        }
        PathPanel.Children.Clear();
        var full = _shellContext.CurrentDirectory.FullName;
        var parts = full.Split(Path.DirectorySeparatorChar);
        // Build up cumulative paths. Start with root (e.g. C:\)
        string accum = parts[0] + Path.DirectorySeparatorChar;
        for (int i = 0; i < parts.Length; i++)
        {
            var btn = new System.Windows.Controls.Button
            {
                Content = parts[i],
                Margin = new Thickness(2),
                Padding = new Thickness(4),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray,
                Tag = accum
            };
            btn.Click += async (_, _) =>
            {
                CommandTextBox.Text = $"cd \"{btn.Tag}\"";
                await ExecuteCurrentCommandAsync();
                CommandTextBox.Text = "ls";
                await ExecuteCurrentCommandAsync();
            };
            PathPanel.Children.Add(btn);
            if (i < parts.Length - 1)
            {
                accum += parts[i + 1] + Path.DirectorySeparatorChar;
            }
        }
    }

    /// <summary>
    /// Deletes the selected item via context menu. Executes the del command and then
    /// refreshes the directory listing.
    /// </summary>
    private async void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsListView.SelectedItem is not FileItem item)
        {
            return;
        }
        CommandTextBox.Text = $"del \"{item.FullPath}\"";
        await ExecuteCurrentCommandAsync();
        CommandTextBox.Text = "ls";
        await ExecuteCurrentCommandAsync();
    }

    /// <summary>
    /// Renames the selected item via context menu. Presents a rename dialog to the user,
    /// executes the rename command, then refreshes the directory listing.
    /// </summary>
    private async void RenameItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsListView.SelectedItem is not FileItem item)
        {
            return;
        }
        // Show rename dialog with the current name pre-filled. If the user cancels, do nothing.
        var dialog = new RenameDialog(item.Name);
        dialog.Owner = this;
        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }
        var newName = dialog.NewName;
        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }
        CommandTextBox.Text = $"rename \"{item.FullPath}\" \"{newName}\"";
        await ExecuteCurrentCommandAsync();
        CommandTextBox.Text = "ls";
        await ExecuteCurrentCommandAsync();
    }
}
