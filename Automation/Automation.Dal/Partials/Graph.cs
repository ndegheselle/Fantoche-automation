using System.Drawing;

namespace Automation.Models.Work
{
    public partial class GraphNode
    {
        // HACK : This is a workaround so that the backend use System.Drawing.Point instead of System.Windows.Point
        public Point Position { get; set; }
    }
}
