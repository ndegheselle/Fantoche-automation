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
        private IModalContentFeedback? _content;
        public Modal()
        {
            InitializeComponent();
        }

        public void Close()
        {
            this.Visibility = Visibility.Collapsed;
            ContentContainer.Content = null;
        }

        public void Show<T>(string title, IModalContent<T> content)
        {
            if (content is IModalContentFeedback feedback)
                _content = feedback;

            ModalTitle.Text = title;
            ContentContainer.Content = content;
            this.Visibility = Visibility.Visible;

            // Wait for onfinish event and return result
            // Allow cancel from close
        }

        private void ButtonClose(object sender, RoutedEventArgs e)
        {
            if (_content != null)
                _content.OnClose();
            Close();
        }
    }
}
