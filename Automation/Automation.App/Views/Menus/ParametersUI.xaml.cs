using Automation.App.Base;
using Automation.App.Contexts;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

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
