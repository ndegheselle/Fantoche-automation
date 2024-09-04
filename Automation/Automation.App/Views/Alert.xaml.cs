using Automation.App.Base;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Automation.App.Views
{
    /// <summary>
    /// Logique d'interaction pour Alert.xaml
    /// </summary>
    public partial class Alert : UserControl, IAlert
    {
        private readonly AlertOptions _options = new AlertOptions();

        private Storyboard _progressStoryboard => (Storyboard)FindResource("FillProgressBar");
        private Storyboard _displayStoryboard => (Storyboard)FindResource("SlideInFromTop");
        public Alert()
        {
            this.DataContext = _options;
            InitializeComponent();
            _progressStoryboard.Completed += (s, e) =>
            {
                Hide();
            };
        }

        #region IAlert

        public void Show(EnumDialogType type, string message)
        {
            _options.Message = message;
            _options.Type = type;
            this.Visibility = Visibility.Visible;
            Animate();
        }

        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
        }

        public void Error(string message)
        {
            Show(EnumDialogType.Error, message);
        }

        public void Info(string message)
        {
            Show(EnumDialogType.Info, message);
        }

        public void Success(string message)
        {
            Show(EnumDialogType.Success, message);
        }

        public void Warning(string message)
        {
            Show(EnumDialogType.Warning, message);
        }

        #endregion


        private void Animate()
        {
            _displayStoryboard.Seek(TimeSpan.Zero);
            _displayStoryboard.Begin(AlertContainer);
            
            _progressStoryboard.Seek(TimeSpan.Zero);
            _progressStoryboard.Begin(ProgressBarTimer);

        }

        #region UI events
        private void ButtonClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Hide();
        }
        #endregion
    }
}
