using Automation.Shared.Data;
using System.Text.Json.Serialization;
using Avalonia;

namespace Automation.Models.Work
{
    public partial class TaskConnector
    {
        [JsonIgnore]
        public bool IsConnected { get; set; }
        [JsonIgnore]
        public Point Anchor { get; set; }
    }
}
