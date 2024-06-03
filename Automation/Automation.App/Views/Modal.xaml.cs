using Automation.App.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
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

namespace Automation.App.Views
{
    /// <summary>
    /// Logique d'interaction pour Modal.xaml
    /// </summary>
    public partial class Modal : UserControl, IModalContainer
    {
        public Modal()
        {
            InitializeComponent();
        }

        public void Close()
        {
            this.Visibility = Visibility.Collapsed;
            ContentContainer.Content = null;
        }

        public void Show(FrameworkElement content)
        {
            ContentContainer.Content = content;
            this.Visibility = Visibility.Visible;
        }

        private void ButtonClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
