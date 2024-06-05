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
        public event Action<bool>? OnClose;
        private TaskCompletionSource<bool>? _taskCompletionSource = null;

        public Modal()
        {
            InitializeComponent();
        }

        public void Close(bool result = false)
        {
            if (_taskCompletionSource == null)
                return;

            OnClose?.Invoke(result);
            _taskCompletionSource.SetResult(result);
            _taskCompletionSource = null;
            // Hide the modal
            Visibility = Visibility.Collapsed;
            ContentContainer.Content = null;
        }

        public Task<bool> Show(string title, IModalContent content)
        {
            content.ModalParent = this;
            ContentContainer.Content = content;
            ModalTitle.Text = title;
            this.Visibility = Visibility.Visible;

            _taskCompletionSource = new TaskCompletionSource<bool>();
            return _taskCompletionSource.Task;
        }

        private void ButtonClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
