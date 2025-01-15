using Joufflu.Popups;
using System.Windows.Controls;
using System.Windows.Data;
using Usuel.Shared;

namespace Automation.App.Components.Inputs
{
    public partial class TextBoxModal : UserControl, IModalContent
    {
        public Modal? ParentLayout { get; set; }
        public ModalOptions Options { get; private set; } = new ModalOptions()
        {
            Title = "Input data"
        };
        public string SubTitle { get; set; } = string.Empty;
        public ICustomCommand ValidateCommand { get; protected set; }

        public TextBoxModal(string titre)
        {
            Options.Title = titre;
            ValidateCommand = new DelegateCommand(() => ParentLayout?.Hide(true));
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
