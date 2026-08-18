using Joufflu.Controls;

namespace Automation.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow(ShellViewModel shell)
        {
            this.DataContext = shell;
            InitializeComponent();
        }
    }
}