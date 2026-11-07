using Fantoche.Shared.Data.Execution;

namespace Fantoche.Shared.Data.Scoped;

/// <summary>
/// An interface of a task : what the user edits its parameters with, in the editor and when a run
/// asks the user. An element of the tree like a task, pointing at a class implementing
/// <c>ITaskInterface</c> in a package of interfaces.
/// <para>
/// It is linked to a task or a workflow by hanging under it (<see cref="ScopedElement.ParentId"/>) :
/// the element then shows and asks with it (see <see cref="Pick"/>), and the tree shows it as its
/// child. An element with no interface under it uses <see cref="Form"/>, the form generated from its
/// parameters.
/// </para>
/// </summary>
public class AutomationInterface : ScopedElement
{
    /// <summary>
    /// The interface [settings] pick among [linked], the interfaces hanging under the element, null
    /// for the generated form. By default the first one pointing at a class, the form when none
    /// does. A pick that can't be shown anymore (unlinked, removed, its class removed) falls back on
    /// that default rather than on the form : the element keeps the interface it was given.
    /// </summary>
    public static AutomationInterface? Pick(TaskSettings settings, IEnumerable<AutomationInterface> linked)
    {
        if (settings.InterfaceId == Form.Id)
            return null;

        List<AutomationInterface> showable = [.. linked.Where(x => x.Target != null)];
        AutomationInterface? picked = showable.FirstOrDefault(x => x.Id == settings.InterfaceId);
        return picked ?? showable.FirstOrDefault();
    }

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
