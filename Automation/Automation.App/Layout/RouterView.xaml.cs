using Automation.App.ViewModels;
using Automation.App.Views.Menus;
using Automation.App.Views.Scope;
using Automation.App.Views.Task;
using Automation.App.Views.Workflow;
using Automation.Base;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Automation.App.Layout
{
    /// <summary>
    /// Experimenting a system without a navigation service Change content based on the selected item in the side menu
    /// </summary>
    public partial class RouterView : UserControl
    {
        private readonly SideMenuContext SideMenuContext = GlobalContext.Instance.SideMenu;

        public RouterView()
        {
            SideMenuContext.PropertyChanged += SideMenuContext_PropertyChanged;

            InitializeComponent();
        }

        private void SideMenuContext_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SideMenuContext.SelectedElement))
                return;
            if (SideMenuContext.SelectedElement == null)
                return;

            if (SideMenuContext.SelectedElement is TaskScope scope)
            {
                this.Content =  new ScopePage(scope);
            }
            else if (SideMenuContext.SelectedElement is TaskWorkflow workflow)
            {
                this.Content =  new WorkflowPage(workflow);
            }
            else if (SideMenuContext.SelectedElement is ITask task)
            {
                this.Content =  new TaskPage(task);
            }
        }
    }
}
