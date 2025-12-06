using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using ShellTotalCommander1.Shell;
using ShellTotalCommander1.Shell.Commands;
using ShellTotalCommander1.Logging;
using System.Threading.Tasks;
using ShellTotalCommander1.ServerClient;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace ShellTotalCommander1
{
    public partial class MainWindow : Window
    {
        private readonly ShellContext _leftContext = new();
        private readonly ShellContext _rightContext = new();
        private readonly DatabaseLogger _logger = new();
        private readonly ShellServerClient _serverClient = new();

        private bool _serverUnavailable;
        private DateTime _serverRetryUntil;

        private readonly Stack<string> _leftBackHistory = new();
        private readonly Stack<string> _rightBackHistory = new();

        private enum PanelSide
        {
            Left,
            Right
        }

        private PanelSide _activePanel = PanelSide.Left;

        private ShellContext GetContext(PanelSide side) =>
            side == PanelSide.Left ? _leftContext : _rightContext;

        private ListView GetListView(PanelSide side) =>
            side == PanelSide.Left ? ItemsListViewLeft : ItemsListViewRight;

        private Stack<string> GetHistory(PanelSide side) =>
            side == PanelSide.Left ? _leftBackHistory : _rightBackHistory;

        private TextBlock GetPathBlock(PanelSide side) =>
            side == PanelSide.Left ? LeftPathTextBlock : RightPathTextBlock;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializePanels();
            CommandTextBox.Focus();
        }

        private void InitializePanels()
        {
            try
            {
                if (Directory.Exists(@"C:\"))
                {
                    _leftContext.ChangeDirectory(@"C:\");
                }
            }
            catch
            {
                // Ignore failures, fall back to default current directory.
            }

            try
            {
                if (Directory.Exists(@"D:\"))
                {
                    _rightContext.ChangeDirectory(@"D:\");
                }
                else
                {
                    _rightContext.ChangeDirectory(_leftContext.CurrentDirectory.FullName);
                }
            }
            catch
            {
                // Ignore failures.
            }

            RefreshPanel(PanelSide.Left);
            RefreshPanel(PanelSide.Right);
            UpdateActivePanelVisual();
        }

        private void RefreshPanel(PanelSide side)
        {
            var context = GetContext(side);
            var listView = GetListView(side);

            try
            {
                var result = context.Execute("ls");
                listView.ItemsSource = result.Items.ToList();
                UpdatePathHeader(side);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Foreground = Brushes.DarkRed;
                StatusTextBlock.Text = ex.Message;
            }
        }

        private void UpdatePathHeader(PanelSide side)
        {
            var block = GetPathBlock(side);
            var ctx = GetContext(side);
            try
            {
                block.Text = ctx.CurrentDirectory.FullName;
            }
            catch
            {
                block.Text = string.Empty;
            }
        }

        private void UpdateActivePanelVisual()
        {
            var activeBrush = Brushes.DodgerBlue;
            var inactiveBrush = (Brush)new BrushConverter().ConvertFromString("#666666")!;

            LeftPanelBorder.BorderBrush = _activePanel == PanelSide.Left ? activeBrush : inactiveBrush;
            RightPanelBorder.BorderBrush = _activePanel == PanelSide.Right ? activeBrush : inactiveBrush;
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
                return;
            }

            try
            {
                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var commandName = parts.Length > 0 ? parts[0] : string.Empty;
                var args = parts.Skip(1).ToArray();

                var context = GetContext(_activePanel);

                CommandResponse? response = null;
                bool attemptedRemote = false;

                if (!_serverUnavailable || DateTime.Now >= _serverRetryUntil)
                {
                    attemptedRemote = true;
                    response = await _serverClient.SendCommandAsync(commandName, args, context.CurrentDirectory.FullName);
                }

                if (response is not null)
                {
                    _serverUnavailable = false;
                    _serverRetryUntil = default;

                    if (!string.IsNullOrWhiteSpace(response.CurrentDirectory))
                    {
                        try
                        {
                            context.ChangeDirectory(response.CurrentDirectory);
                        }
                        catch
                        {
                            // Ignore sync failures
                        }
                    }

                    ApplyRemoteResult(response, _activePanel);
                    _ = _logger.LogAsync(commandName, args, response.Success, response.Message);
                    return;
                }

                if (attemptedRemote)
                {
                    _serverUnavailable = true;
                    _serverRetryUntil = DateTime.Now.AddSeconds(5);
                }

                var localResult = context.Execute(input);
                var prefixedMessage = $"[Локально] {localResult.Message}";
                _ = _logger.LogAsync(commandName, args, localResult.Success, prefixedMessage);

                StatusTextBlock.Foreground = localResult.Success ? Brushes.DarkGreen : Brushes.DarkRed;
                StatusTextBlock.Text = prefixedMessage;

                var listView = GetListView(_activePanel);
                listView.ItemsSource = localResult.Items.ToList();
                UpdatePathHeader(_activePanel);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Foreground = Brushes.DarkRed;
                StatusTextBlock.Text = ex.Message;
            }
        }

        private void ApplyRemoteResult(CommandResponse response, PanelSide side)
        {
            StatusTextBlock.Foreground = response.Success ? Brushes.DarkGreen : Brushes.DarkRed;
            var displayMessage = $"[Сервер] {response.Message}";
            StatusTextBlock.Text = displayMessage;

            var listView = GetListView(side);

            if (response.Items is not null)
            {
                var items = response.Items
                    .Select(path =>
                    {
                        try
                        {
                            if (Directory.Exists(path))
                                return new FileItem(new DirectoryInfo(path));
                            if (File.Exists(path))
                                return new FileItem(new FileInfo(path));
                        }
                        catch
                        {
                        }
                        return null;
                    })
                    .Where(item => item is not null)!
                    .ToList();

                listView.ItemsSource = items;
            }
            else
            {
                listView.ItemsSource = Array.Empty<object>();
            }

            UpdatePathHeader(side);
        }

        private void SetActivePanelFromSender(object sender)
        {
            if (sender is ListView lv)
            {
                if (ReferenceEquals(lv, ItemsListViewLeft))
                    _activePanel = PanelSide.Left;
                else if (ReferenceEquals(lv, ItemsListViewRight))
                    _activePanel = PanelSide.Right;
            }
            UpdateActivePanelVisual();
        }

        private async void ItemsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListView listView || listView.SelectedItem is not FileItem selected)
                return;

            SetActivePanelFromSender(sender);

            var side = _activePanel;
            var context = GetContext(side);
            var history = GetHistory(side);

            if (selected.IsDirectory)
            {
                try
                {
                    history.Push(context.CurrentDirectory.FullName);
                }
                catch
                {
                }

                CommandTextBox.Text = $"cd \"{selected.FullPath}\"";
                await ExecuteCurrentCommandAsync();

                CommandTextBox.Text = "ls";
                await ExecuteCurrentCommandAsync();
            }
            else
            {
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

        private async void UpButton_Click(object sender, RoutedEventArgs e)
        {
            var context = GetContext(_activePanel);
            var history = GetHistory(_activePanel);

            try
            {
                history.Push(context.CurrentDirectory.FullName);
            }
            catch
            {
            }

            CommandTextBox.Text = "cd ..";
            await ExecuteCurrentCommandAsync();

            CommandTextBox.Text = "ls";
            await ExecuteCurrentCommandAsync();
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var history = GetHistory(_activePanel);
            if (history.Count == 0)
            {
                StatusTextBlock.Foreground = Brushes.DarkRed;
                StatusTextBlock.Text = "Немає попередньої директорії для повернення.";
                return;
            }

            var previous = history.Pop();
            CommandTextBox.Text = $"cd \"{previous}\"";
            await ExecuteCurrentCommandAsync();

            CommandTextBox.Text = "ls";
            await ExecuteCurrentCommandAsync();
        }

        private PanelSide GetSideFromContextMenuSender(object sender)
        {
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu ctxMenu &&
                ctxMenu.PlacementTarget is ListView lv)
            {
                if (ReferenceEquals(lv, ItemsListViewLeft))
                    return PanelSide.Left;
                if (ReferenceEquals(lv, ItemsListViewRight))
                    return PanelSide.Right;
            }
            return _activePanel;
        }

        private async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var side = GetSideFromContextMenuSender(sender);
            var listView = GetListView(side);

            if (listView.SelectedItem is not FileItem item)
                return;

            _activePanel = side;
            UpdateActivePanelVisual();

            CommandTextBox.Text = $"del \"{item.FullPath}\"";
            await ExecuteCurrentCommandAsync();

            CommandTextBox.Text = "ls";
            await ExecuteCurrentCommandAsync();
        }

        private async void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            var side = GetSideFromContextMenuSender(sender);
            var listView = GetListView(side);

            if (listView.SelectedItem is not FileItem item)
                return;

            _activePanel = side;
            UpdateActivePanelVisual();

            var dialog = new RenameDialog(item.Name)
            {
                Owner = this
            };
            var result = dialog.ShowDialog();
            if (result != true)
                return;

            var newName = dialog.NewName;
            if (string.IsNullOrWhiteSpace(newName))
                return;

            CommandTextBox.Text = $"rename \"{item.FullPath}\" \"{newName}\"";
            await ExecuteCurrentCommandAsync();

            CommandTextBox.Text = "ls";
            await ExecuteCurrentCommandAsync();
        }
    }
}
