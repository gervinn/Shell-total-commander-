using System.Windows;

namespace ShellTotalCommander1
{
    /// <summary>
    /// Interaction logic for RenameDialog.xaml
    /// </summary>
    public partial class RenameDialog : Window
    {
        /// <summary>
        /// Gets the name entered by the user. This property will be used
        /// by the caller to retrieve the new file or directory name.
        /// </summary>
        public string NewName => NameBox.Text;

        public RenameDialog(string originalName)
        {
            InitializeComponent();
            // Prefill the text box with the original name for convenience.
            NameBox.Text = originalName;
            NameBox.SelectAll();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}