using Joufflu.Controls;

namespace Fantoche.App
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