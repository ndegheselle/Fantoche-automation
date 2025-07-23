using Automation.Shared.Data;
using System.Text.Json.Serialization;
using System.Windows;

namespace Automation.Dal.Models
{
    public partial class TaskConnector
    {
        [JsonIgnore]
        public bool IsConnected { get; set; }
        [JsonIgnore]
        public Point Anchor { get; set; }
    }
}
