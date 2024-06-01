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

namespace Automation.App.Views.Scope
{
    /// <summary>
    /// Logique d'interaction pour ScopePage.xaml
    /// </summary>
    public partial class ScopePage : UserControl
    {
        public TaskScope Scope { get; set; }

        public ScopePage(TaskScope scope)
        {
            Scope = scope;
            InitializeComponent();
            this.DataContext = Scope;
        }
    }
}
