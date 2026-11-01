using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Nodify;

namespace Fantoche.App.Features.Workflows.Editor
{
    /// <summary>
    /// Nodify bezier connection, with a flatter curve when both ends are vertical (a page plugged
    /// above its task). Nodify bends its handles by at least 100px whatever the distance, which
    /// makes the short attachment loop on itself : its handles here meet at half the height.
    /// </summary>
    public class EditorConnection : Connection
    {
        protected override ((Point ArrowStartSource, Point ArrowStartTarget), (Point ArrowEndSource, Point ArrowEndTarget)) DrawLineGeometry(StreamGeometryContext context, Point source, Point target)
        {
            bool isVertical = SourceOrientation == Orientation.Vertical && TargetOrientation == Orientation.Vertical;
            if (!isVertical)
                return base.DrawLineGeometry(context, source, target);

            var handle = new Vector(0, (target.Y - source.Y) / 2);

            context.BeginFigure(source, false, false);
            context.BezierTo(source + handle, target - handle, target, true, true);

            return ((target, source), (source, target));
        }
    }
}
