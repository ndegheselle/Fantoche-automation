using Joufflu.Popups;
using System.Windows.Controls;
using Usuel.Shared;

namespace Automation.App.Components.Inputs
{
    /// <summary>
    /// Logique d'interaction pour SelectColorModal.xaml
    /// </summary>
    public partial class SelectColorModal : UserControl, IModalContent
    {
        public IModal? ParentLayout { get; set; }
        public ModalOptions Options { get; } = new ModalOptions() { Title = "Select color" };

        public ICustomCommand SelectCommand { get; }
        public string? Selected { get; set; }

        public List<string> Colors { get; private set; } = [
            "#001219",
            "#005F73",
            "#0A9396",
            "#94D2BD",
            "#E9D8A6",
            "#EE9B00",
            "#CA6702", 
            "#BB3E03", 
            "#AE2012", 
            "#9B2226",
            "#2B2D42",
            "#8D99AE",
            "#EDF2F4",
            "#EF233C",
            "#D90429",
            "#FF595E",
            "#FFCA3A",
            "#8AC926",
            "#1982C4",
            "#6A4C93",
        ];

        public SelectColorModal()
        {
            SelectCommand = new DelegateCommand(() => ParentLayout?.Hide(true));
            InitializeComponent();
        }
    }
}
