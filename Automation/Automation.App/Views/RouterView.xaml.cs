using Automation.App.Base;
using Automation.App.ViewModels;
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
        private readonly SideMenuContext _sideMenuContext;
        private readonly App _app = (App)App.Current;

        public RouterView()
        {
            _sideMenuContext = _app.ServiceProvider.GetRequiredService<SideMenuContext>();
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
                case EnumTaskType.Scope:
                    this.Content = new ScopePage(_app.ServiceProvider.GetRequiredService<IModalContainer>(), (Scope)_sideMenuContext.SelectedElement);
                    break;
                case EnumTaskType.Workflow:
                    this.Content = new WorkflowPage(_sideMenuContext.SelectedElement);
                    break;
                case EnumTaskType.Task:
                    this.Content = new TaskPage(_sideMenuContext.SelectedElement);
                    break;
            }
        }
    }
}
