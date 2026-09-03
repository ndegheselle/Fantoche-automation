using System.Windows.Controls;

namespace Automation.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// Displays the usages of the <see cref="UsagesViewModel"/> set as its DataContext. They are read
    /// again every time it is displayed (e.g. when its tab is selected) : a graph edited elsewhere
    /// doesn't report the nodes it added or removed.
    /// </summary>
    public partial class UsagesTable : UserControl
    {
        public UsagesViewModel? ViewModel => DataContext as UsagesViewModel;

        public UsagesTable()
        {
            InitializeComponent();
        }
    }
}
