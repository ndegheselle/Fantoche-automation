using Automation.App.Views.TasksPages.TaskUI;
using Automation.Shared.ViewModels;
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

namespace Automation.App.Views.TasksPages.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowEdit.xaml
    /// </summary>
    public partial class WorkflowEdit : UserControl
    {
        public static readonly DependencyProperty WorkflowProperty =
            DependencyProperty.Register(nameof(Workflow), typeof(WorkflowNode), typeof(WorkflowEdit), new PropertyMetadata(null));

        public WorkflowNode Workflow
        {
            get { return (WorkflowNode)GetValue(WorkflowProperty); }
            set { SetValue(WorkflowProperty, value); }
        }

        public WorkflowEdit()
        {
            InitializeComponent();
        }
    }
}
