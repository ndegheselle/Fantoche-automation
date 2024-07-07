using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components.Inputs
{
    /// <summary>
    /// Logique d'interaction pour FilePicker.xaml
    /// </summary>
    public partial class FilePicker : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register(
            "FilePath",
            typeof(string),
            typeof(FilePicker),
            new PropertyMetadata(string.Empty));

        public string FilePath
        {
            get { return (string)GetValue(FilePathProperty); }
            set { SetValue(FilePathProperty, value); }
        }

        public string Filter { get; set; }

        #endregion

        public FilePicker() { InitializeComponent(); }

        #region UI Events

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            FilePath = null;
        }
        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            // Open file dialog
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = Filter;
            if (dialog.ShowDialog() == true)
            {
                FilePath = dialog.FileName;
            }
        }

        #endregion
    }
}
