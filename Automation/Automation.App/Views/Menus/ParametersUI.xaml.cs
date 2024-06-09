using Automation.App.Base;
using Automation.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
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

namespace Automation.App.Views.Menus
{
    /// <summary>
    /// Logique d'interaction pour ParametersUI.xaml
    /// </summary>
    public partial class ParametersUI : UserControl, IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
        private readonly App _app = (App)App.Current;
        private readonly ParametersContext _context;

        public ParametersUI()
        {
            _context = _app.ServiceProvider.GetRequiredService<ParametersContext>();
            InitializeComponent();
            this.DataContext = _context;
        }
    }
}
