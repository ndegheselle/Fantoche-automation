using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Remote;

/// <summary>A task to start on the workers, see <see cref="Services.IDispatchService.StartAsync"/>.</summary>
public sealed class StartRequest
{
    public string Name { get; set; } = "";

    public JToken? Parameters { get; set; }
}
