using Nodify.Interactivity;
using System.Windows.Input;

namespace Automation.App.Views.WorkPages.Workflows.Editor.Gestures
{
    public class LockedGestureMappings : EditorGestures
    {
        public static readonly LockedGestureMappings Instance = new LockedGestureMappings();

        public LockedGestureMappings()
        {
            Editor.Selection.Apply(SelectionGestures.None);
            ItemContainer.Selection.Apply(SelectionGestures.None);
            Connection.Disconnect.Value = MultiGesture.None;
            Connector.Connect.Value = MultiGesture.None;
            Editor.Pan.Value = new AnyGesture(new System.Windows.Input.MouseGesture(MouseAction.MiddleClick));
        }
    }
}
