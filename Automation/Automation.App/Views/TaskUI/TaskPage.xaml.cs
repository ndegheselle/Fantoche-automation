using Automation.App.Base;
using Automation.App.Views.ScopeUI;
using Automation.Shared.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TaskUI
{
    /// <summary>
    /// Logique d'interaction pour TaskPage.xaml
    /// </summary>
    public partial class TaskPage : UserControl
    {
        private readonly IModalContainer _modal;
        private readonly ScopedNode _scope;

        public TaskPage(IModalContainer modal, ScopedNode scope)
        {
            _scope = scope;
            _modal = modal;
            this.DataContext = _scope;
            InitializeComponent();
        }

        private void ButtonParam_Click(object sender, RoutedEventArgs e)
        {
            _modal.Show(new ScopedEditModal(_scope));
        }
    }
}
