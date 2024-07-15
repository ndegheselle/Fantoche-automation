using Automation.App.Base;
using Automation.Shared.ViewModels;
using System.Windows;
using System.Windows.Controls;
using static System.Formats.Asn1.AsnWriter;

namespace Automation.App.Views.TasksPages.Components
{
    public class ScopedParametersModal : ScopedParameters, IModalContentCallback
    {
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Edit scoped", ValidButtonText = "Save" };

        public ScopedParametersModal(ScopedElement scope) : base(scope)
        {
            if (scope.Id == Guid.Empty)
                Options.Title = "New scoped";
        }

        public void OnModalClose(bool result)
        {
            // Not a new scope anymore !
            if (Scoped.Id == Guid.Empty)
                Scoped.Id = Guid.NewGuid();
        }
    }


    /// <summary>
    /// Logique d'interaction pour ScopedParameters.xaml
    /// </summary>
    public partial class ScopedParameters : UserControl
    {
        public static readonly DependencyProperty ScopedProperty =
            DependencyProperty.Register(
            nameof(Scoped),
            typeof(ScopedElement),
            typeof(ScopedParameters),
            new PropertyMetadata(null));

        public ScopedElement Scoped
        {
            get { return (ScopedElement)GetValue(ScopedProperty); }
            set { SetValue(ScopedProperty, value); }
        }

        public ScopedParameters(ScopedElement scoped)
        {
            Scoped = scoped;
            InitializeComponent();
        }

        public ScopedParameters() { InitializeComponent(); }
    }
}
