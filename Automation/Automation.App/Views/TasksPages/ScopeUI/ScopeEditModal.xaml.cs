using Automation.App.Components.Inputs;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared.Base;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.ScopeUI
{
    public class ScopeCreateModal : TextBoxModal, IModalContentValidation
    {
        private readonly App _app = App.Current;
        private readonly ScopesClient _scopeClient;
        public Scope NewScope { get; set; }

        public ScopeCreateModal(Scope scope) : base("Create new scope")
        {
            _scopeClient = _app.ServiceProvider.GetRequiredService<ScopesClient>();
            NewScope = scope;
            BindValue(nameof(Scope.Name), NewScope);
        }

        public async Task<bool> OnValidation()
        {
            NewScope.ClearErrors();
            try
            {
                NewScope.Id = await _scopeClient.CreateAsync(NewScope);
            }
            catch (ValidationException ex)
            {
                if (ex.Errors != null)
                    NewScope.AddErrors(ex.Errors);
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Logique d'interaction pour ScopedParameters.xaml
    /// </summary>
    public partial class ScopeEditModal : UserControl, IModalContentValidation
    {
        public Modal? ParentLayout { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Scope settings" };

        private readonly App _app = (App)App.Current;
        private readonly ScopesClient _scopeClient;
        private IAlert _alert => this.GetCurrentAlertContainer();

        public Scope Scope { get; set; }

        public ScopeEditModal(Scope scope) {
            Scope = scope;
            _scopeClient = _app.ServiceProvider.GetRequiredService<ScopesClient>();
            InitializeComponent(); 
        }

        public async Task<bool> OnValidation()
        {
            Scope.ClearErrors();
            try
            {
                await _scopeClient.UpdateAsync(Scope.Id, Scope);
                _alert.Success("Scope updated !");
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
}
