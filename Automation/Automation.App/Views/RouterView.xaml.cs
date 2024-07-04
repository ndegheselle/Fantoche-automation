using Automation.App.Base;
using Automation.App.ViewModels;
using Automation.App.ViewModels.Graph;
using Automation.App.Views.ScopeUI;
using Automation.App.Views.TaskUI;
using Automation.App.Views.WorkflowUI;
using Automation.Base.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views
{
    /// <summary>
    /// Experimenting a system without a navigation service Change content based on the selected item in the side menu
    /// </summary>
    public partial class RouterView : UserControl
    {
        #region Dependency properties
        // Dependency property selected node
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            "Selected",
            typeof(ScopedElement),
            typeof(RouterView),
            new PropertyMetadata(null, (o, e) => ((RouterView)o).OnSelectedNodeChanged()));

        public ScopedElement Selected
        {
            get { return (ScopedElement)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        void OnSelectedNodeChanged()
        {
            if (Selected == null)
                return;

            switch (Selected.Type)
            {
                case EnumScopedType.Scope:
                    this.Content = new ScopePage(
                        _app.ServiceProvider.GetRequiredService<IModalContainer>(),
                        (Scope)Selected);
                    break;
                case EnumScopedType.Workflow:
                    this.Content = new WorkflowPage((WorkflowNode)Selected);
                    break;
                case EnumScopedType.Task:
                    this.Content = new TaskPage((TaskNode)Selected);
                    break;
            }
        }
        #endregion

        private readonly App _app = (App)App.Current;

        public RouterView() { InitializeComponent(); }
    }
}
