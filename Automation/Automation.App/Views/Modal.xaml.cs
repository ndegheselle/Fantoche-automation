using Automation.App.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views
{
    /// <summary>
    /// Logique d'interaction pour Modal.xaml
    /// </summary>
    public partial class Modal : UserControl, IModalContainer
    {
        public ModalOptions Options { get; set; } = new ModalOptions();
        public event Action<bool>? OnClose;
        private TaskCompletionSource<bool>? _taskCompletionSource = null;
        private IModalContent? _content;

        public Modal()
        {
            InitializeComponent();
        }

        public void Close(bool result = false)
        {
            if (_taskCompletionSource == null)
                return;

            if (_content is IModalContentCallback callback)
                callback.OnModalClose(result);

            OnClose?.Invoke(result);
            _taskCompletionSource.SetResult(result);
            _taskCompletionSource = null;
            // Hide the modal
            Visibility = Visibility.Collapsed;
            ContentContainer.Content = null;
            _content = null;
        }

        public Task<bool> Show(IModalContent content, ModalOptions? options = null)
        {
            _content = content;
            _content.ModalParent = this;
            ContentContainer.Content = _content;
            Options = options ?? _content.Options;
            this.DataContext = Options;
            this.Visibility = Visibility.Visible;

            _taskCompletionSource = new TaskCompletionSource<bool>();
            return _taskCompletionSource.Task;
        }

        private void ButtonClose(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonValidate_Click(object sender, RoutedEventArgs e)
        {
            Close(true);
        }
    }
}
