using Fantoche.Shared.Data.Execution;

namespace Fantoche.Shared.Data.Scoped;

/// <summary>
/// An interface of a task : what the user edits its parameters with, in the editor and when a run
/// asks the user. An element of the tree like a task, pointing at a class implementing
/// <c>ITaskInterface</c> in a package of interfaces.
/// <para>
/// It is linked to a task by hanging under it (<see cref="ScopedElement.ParentId"/>) : the task then
/// shows and asks with it (see <see cref="BaseAutomationTask.Interface"/>), and the tree shows it as
/// the child of the task. A task with no interface under it uses <see cref="Form"/>, the form
/// generated from its parameters.
/// </para>
/// </summary>
public class AutomationInterface : ScopedElement
{
    /// <summary>
    /// The form generated from the parameters of a task, built in : the interface of a task with
    /// none of its own. In the controls scope, like the start and the end of a workflow, from where
    /// it is dragged onto a node to ask the user.
    /// </summary>
    public static readonly AutomationInterface Form = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-100000000006"),
        ParentId = Scope.Controls.Id,
        Metadata = new ScopedMetadata(EnumScopedType.Interface) { Tags = ["Control"], Name = "Form", IsReadOnly = true },
    };

    /// <summary>The class implementing the interface, null for <see cref="Form"/>.</summary>
    public PackageClassTarget? Target { get; set; }

    public bool IsForm => Id == Form.Id;

    public AutomationInterface() : base(EnumScopedType.Interface)
    {
    }

    /// <param name="parentId">A task to link it to it, a scope otherwise.</param>
    public AutomationInterface(string name, Guid parentId) : base(new ScopedMetadata(name, EnumScopedType.Interface))
    {
        ParentId = parentId;
    }
}
