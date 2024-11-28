using Joufflu.Popups;
using System.Windows.Controls;
using System.Windows.Data;

namespace Automation.App.Components.Inputs
{
    public partial class TextBoxModal : UserControl, IModalContent
    {
        public Modal? ParentLayout { get; set; }
        public ModalOptions Options => new ModalOptions()
        {
            Title = "Input data"
        };
        public string SubTitle { get; set; } = string.Empty;

        public TextBoxModal(string titre)
        {
            Options.Title = titre;
            InitializeComponent();
        }

        // That's a little bit gadget
        // Used to allow inheritance of TextBoxModal and have a property bind directly on the textbox
        // Allow for INotifyDataErrorInfo handling without too much hassle
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
