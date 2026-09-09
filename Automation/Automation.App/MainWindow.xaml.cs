using Joufflu.Controls;

namespace Automation.App
{
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow(SpineViewModel shell)
        {
            this.DataContext = shell;
            InitializeComponent();
        }
    }
}