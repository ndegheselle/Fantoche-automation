using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Automation.App.Base.Components
{
    /// <summary>
    /// Logique d'interaction pour FolderPicker.xaml
    /// </summary>
    public partial class FolderPicker : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty SelectedFolderPathProperty = DependencyProperty.Register(
            "SelectedFilePath", typeof(string), typeof(FolderPicker), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string SelectedFolderPath
        {
            get => (string)GetValue(SelectedFolderPathProperty);
            set => SetValue(SelectedFolderPathProperty, value);
        }

        public FolderPicker()
        {
            InitializeComponent();
        }

        private void OpenFolderPicker_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog
            {};

            if (folderDialog.ShowDialog() == true)
            {
                SelectedFolderPath = folderDialog.FolderName;
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            SelectedFolderPath = string.Empty;
        }
    }
}
