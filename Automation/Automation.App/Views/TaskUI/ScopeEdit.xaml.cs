using Automation.App.Base;
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

namespace Automation.App.Views.TaskUI
{
    /// <summary>
    /// Logique d'interaction pour ScopeEdit.xaml
    /// </summary>
    public partial class ScopeEdit : UserControl, IModalContent
    {
        public event Action<bool>? OnFinish;

        public ScopeEdit()
        {
            InitializeComponent();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            OnFinish?.Invoke(false);
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            OnFinish?.Invoke(true);
        }
    }
}
