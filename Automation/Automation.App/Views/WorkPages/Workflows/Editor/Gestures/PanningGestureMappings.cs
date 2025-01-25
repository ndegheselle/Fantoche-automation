using Nodify;
using Nodify.Interactivity;
using System.Windows.Input;

namespace Automation.App.Views.WorkPages.Workflows.Editor.Gestures
{

    internal class PanningGestureMappings : EditorGestures
    {
        public static readonly PanningGestureMappings Instance = new PanningGestureMappings();

        public PanningGestureMappings()
        {
            Editor.Selection.Apply(SelectionGestures.None);
            Editor.Cutting.Value = MultiGesture.None;
            ItemContainer.Selection.Apply(SelectionGestures.None);
            ItemContainer.Drag.Value = MultiGesture.None;
            Connector.Connect.Value = MultiGesture.None;
        }
    }
}
