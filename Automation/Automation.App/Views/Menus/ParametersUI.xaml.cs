using Automation.App.ViewModels;
using Joufflu.Popups;
using Joufflu.Shared.Layouts;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Automation.App.Views.Menus
{
    /// <summary>
    /// Logique d'interaction pour ParametersUI.xaml
    /// </summary>
    public partial class ParametersUI : UserControl, IModalContent
    {
        public ModalOptions Options => new ModalOptions() { Title = "Parameters"};
        public Modal? ParentLayout { get; set; }

        private readonly ParametersViewModel _context;

        public ParametersUI()
        {
            _context = Services.Provider.GetRequiredService<ParametersViewModel>();
            InitializeComponent();
            this.DataContext = _context;
        }
    }
}
