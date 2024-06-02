using Automation.App.ViewModels;
using Automation.App.Views.TaskUI;
using Automation.App.Views.Workflow;
using Automation.Base;
using System.Windows.Controls;

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

            switch (SideMenuContext.SelectedElement.Type)
            {
                case EnumTaskType.Scope:
                    this.Content = new ScopePage((Scope)SideMenuContext.SelectedElement);
                    break;
                case EnumTaskType.Workflow:
                    this.Content = new WorkflowPage(SideMenuContext.SelectedElement);
                    break;
                case EnumTaskType.Task:
                    this.Content = new TaskPage(SideMenuContext.SelectedElement);
                    break;
            }
        }
    }
}
