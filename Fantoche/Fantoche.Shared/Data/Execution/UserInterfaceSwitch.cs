using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Data.Execution;

/// <summary>
/// Whether an execution asks the user, which decides where it runs : in an application, rather
/// than on any worker (see <see cref="IUserInterfaceTask"/>).
/// </summary>
public static class UserInterfaceSwitch
{
    /// <summary>
    /// Whether [task], run with [parameters], asks the user : the property of its input marked with
    /// <see cref="UserInterface.SchemaKey"/> holds true. A workflow never asks itself, its nodes do.
    /// </summary>
    public static bool IsOn(BaseAutomationTask task, JToken? parameters)
    {
        if (task is not AutomationTask || task.InputSchema == null || parameters is not JObject values)
            return false;

        foreach (var (name, property) in task.InputSchema.ActualSchema.ActualProperties)
        {
            bool isSwitch = property.ExtensionData?.ContainsKey(UserInterface.SchemaKey) == true;
            if (isSwitch && values[name] is JValue { Type: JTokenType.Boolean } value && value.Value<bool>())
                return true;
        }
        return false;
    }
}
