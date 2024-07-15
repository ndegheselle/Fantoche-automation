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

namespace Automation.App.Views.TasksPages.TaskUI
{
    /// <summary>
    /// Logique d'interaction pour TaskEdit.xaml
    /// </summary>
    public partial class TaskEdit : UserControl
    {
        public static readonly DependencyProperty ScopeProperty =
            DependencyProperty.Register(nameof(Scope), typeof(ScopedTask), typeof(TaskEdit), new PropertyMetadata(null));

        public ScopedTask Scope
        {
            get { return (ScopedTask)GetValue(ScopeProperty); }
            set { SetValue(ScopeProperty, value); }
        }

        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register(nameof(Task), typeof(TaskNode), typeof(TaskEdit), new PropertyMetadata(null));

        public TaskNode Task
        {
            get { return (TaskNode)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }


        public TaskEdit()
        {
            InitializeComponent();
        }
    }
}
