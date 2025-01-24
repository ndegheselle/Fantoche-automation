using Nodify;
using Nodify.Interactivity;
using System.Windows.Input;

namespace Automation.App.Views.WorkPages.Workflows.Editor.Gestures
{
    public class DefautGestureMappings : EditorGestures
    {
        public static readonly DefautGestureMappings Instance = new DefautGestureMappings();

        private DefautGestureMappings()
        {
            Editor.ZoomModifierKey = ModifierKeys.Control;
        }
    }
}
