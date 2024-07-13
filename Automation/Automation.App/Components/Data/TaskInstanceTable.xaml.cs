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

namespace Automation.App.Components.Data
{
    /// <summary>
    /// Logique d'interaction pour TaskInstanceTable.xaml
    /// </summary>
    public partial class TaskInstanceTable : UserControl
    {

        public static readonly DependencyProperty InstancesProperty =
            DependencyProperty.Register(nameof(Instances), typeof(IList<TaskInstance>), typeof(TaskInstanceTable), null);

        public IList<TaskInstance> Instances
        {
            get { return (IList<TaskInstance>)GetValue(InstancesProperty); }
            set { SetValue(InstancesProperty, value); }
        }

        public TaskInstanceTable() { InitializeComponent(); }
    }
}
