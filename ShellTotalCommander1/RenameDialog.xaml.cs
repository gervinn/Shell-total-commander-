using System.Windows;

namespace ShellTotalCommander1
{

    public partial class RenameDialog : Window
    {

        public string NewName => NameBox.Text;

        public RenameDialog(string originalName)
        {
            InitializeComponent();
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