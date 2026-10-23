using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Joufflu.Feedback;
using Joufflu.Navigation;

namespace Fantoche.App.Features.Workflows.Details
{
    public class ScopeDetailsViewModel : ScopedDetailsViewModel<Scope>
    {
        public Scope Scope => Element;
        public ScopeDetailsViewModel(
            ScopedNode node,
            WorkflowsViewModel parent,
            IScopedService scoped,
            IHistoryService history,
            IOverlayer overlays,
            IToastService toasts)
            : base(node, parent, scoped, history, overlays, toasts)
        { }
    }
}
