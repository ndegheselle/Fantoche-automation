using Joufflu.Shared.Navigation;
using System.Windows.Controls;

namespace Automation.App.Views.WorkersPages
{
    /// <summary>
    /// Logique d'interaction pour WorkerMainPage.xaml
    /// </summary>
    public partial class WorkerMainPage : UserControl, IPage
    {
        public ILayout? ParentLayout { get; set; }
        public WorkerMainPage()
        {
            InitializeComponent();
        }
    }
}
