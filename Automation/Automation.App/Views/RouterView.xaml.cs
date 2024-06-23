using Automation.App.Base;
using Automation.App.ViewModels;
using Automation.App.ViewModels.Graph;
using Automation.App.Views.ScopeUI;
using Automation.App.Views.TaskUI;
using Automation.App.Views.WorkflowUI;
using Automation.Base;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Automation.App.Views
{
    /// <summary>
    /// Experimenting a system without a navigation service Change content based on the selected item in the side menu
    /// </summary>
    public partial class RouterView : UserControl
    {
        private readonly SideMenuViewModel _sideMenuContext;
        private readonly App _app = (App)App.Current;

        public RouterView()
        {
            _sideMenuContext = _app.ServiceProvider.GetRequiredService<SideMenuViewModel>();
            _sideMenuContext.PropertyChanged += SideMenuContext_PropertyChanged;
            InitializeComponent();
        }

        private void SideMenuContext_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(_sideMenuContext.SelectedElement))
                return;
            if (_sideMenuContext.SelectedElement == null)
                return;

            switch (_sideMenuContext.SelectedElement.Type)
            {
                case EnumNodeType.Scope:
                    this.Content = new ScopePage(_app.ServiceProvider.GetRequiredService<IModalContainer>(), (Scope)_sideMenuContext.SelectedElement);
                    break;
                case EnumNodeType.Workflow:
                    this.Content = new WorkflowPage((WorkflowNode)_sideMenuContext.SelectedElement);
                    break;
                case EnumNodeType.Task:
                    this.Content = new TaskPage(_sideMenuContext.SelectedElement);
                    break;
            }
        }
    }
}
