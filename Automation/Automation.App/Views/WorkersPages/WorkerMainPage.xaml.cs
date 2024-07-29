using Joufflu.Shared;
using System.Windows.Controls;

namespace Automation.App.Views.WorkersPages
{
    /// <summary>
    /// Logique d'interaction pour WorkerMainPage.xaml
    /// </summary>
    public partial class WorkerMainPage : UserControl, IPage
    {
        public INavigationLayout? Layout { get; set; }

        public WorkerMainPage()
        {
            InitializeComponent();
        }
    }
}
