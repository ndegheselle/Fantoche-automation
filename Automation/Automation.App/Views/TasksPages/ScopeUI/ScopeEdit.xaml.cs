using Automation.App.Base;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared.Base;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.ScopeUI
{
    public class ScopeEditModal : ScopeEdit, IModalContentValidate
    {
        private readonly App _app = (App)App.Current;
        private readonly ScopeClient _scopeClient;

        public bool PreventClosing { get; set; }
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Edit scoped", ValidButtonText = "Save" };

        public ScopeEditModal(Scope scope) : base(scope)
        {
            _scopeClient = _app.ServiceProvider.GetRequiredService<ScopeClient>();
            if (scope.Id == Guid.Empty)
                Options.Title = "New scoped";
        }

        public async Task<bool> OnValidate()
        {
            Scope.ClearErrors();
            try
            {
                if (Scope.Id == Guid.Empty)
                {
                    Scope.Id = await _scopeClient.CreateAsync(Scope);
                }
                else
                {
                    await _scopeClient.UpdateAsync(Scope.Id, Scope);
                }
            }
            catch (ValidationException ex)
            {
                if (ex.Errors != null)
                    Scope.AddErrors(ex.Errors);
                return false;
            }

            return true;
        }
    }


    /// <summary>
    /// Logique d'interaction pour ScopedParameters.xaml
    /// </summary>
    public partial class ScopeEdit : UserControl
    {
        public static readonly DependencyProperty ScopedProperty =
            DependencyProperty.Register(
            nameof(Scope),
            typeof(Scope),
            typeof(ScopeEdit),
            new PropertyMetadata(null));

        public Scope Scope
        {
            get { return (Scope)GetValue(ScopedProperty); }
            set { SetValue(ScopedProperty, value); }
        }

        public ScopeEdit(Scope scoped)
        {
            Scope = scoped;
            InitializeComponent();
        }

        public ScopeEdit() { InitializeComponent(); }
    }
}
