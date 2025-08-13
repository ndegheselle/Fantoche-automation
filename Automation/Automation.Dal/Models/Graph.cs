using System.Drawing;

namespace Automation.Dal.Models
{
    public partial class GraphNode
    {
        // HACK : This is a workaround sop that the backend use System.Drawing.Point instead of System.Windows.Point
        public Point Position { get; set; }
    }
}
