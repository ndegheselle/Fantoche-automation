using Automation.App.Base;
using System.Windows.Controls;
using System.Windows.Data;

namespace Automation.App.Components.Inputs
{
    public partial class TextBoxModal : UserControl, IModalContent
    {
        public ModalOptions Options { get; private set; } = new ModalOptions()
        {
            Title = "Input data",
            ValidButtonText = "Ok",
            ShowFooter = true
        };
        public IModalContainer? ModalContainer { get; set; }
        public string SubTitle { get; set; }

        public TextBoxModal(string titre)
        {
            Options.Title = titre;
            InitializeComponent();
        }

        protected void BindValue(string propertyName, object source)
        {
            SubTitle = propertyName;

            Binding valueBinding = new Binding(propertyName);
            valueBinding.Source = source;
            valueBinding.Mode = BindingMode.TwoWay;
            TextBoxValue.SetBinding(TextBox.TextProperty, valueBinding);
        }
    }
}
