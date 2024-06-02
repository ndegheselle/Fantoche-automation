using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components
{
    /// <summary>
    /// Logique d'interaction pour ScopedElementIcon.xaml
    /// </summary>
    public partial class ScopedElementIcon : UserControl
    {
        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
            "Type",
            typeof(EnumTaskType),
            typeof(ScopedElementIcon),
            new PropertyMetadata(EnumTaskType.Workflow));

        public EnumTaskType Type
        {
            get { return (EnumTaskType)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        public ScopedElementIcon()
        {
            InitializeComponent();
        }
    }
}
