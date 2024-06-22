using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components
{
    public partial class NodeIcon : UserControl
    {
        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
            "Type",
            typeof(EnumNodeType),
            typeof(NodeIcon),
            new PropertyMetadata(EnumNodeType.Task));

        public EnumNodeType Type
        {
            get { return (EnumNodeType)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        public NodeIcon()
        {
            InitializeComponent();
        }
    }
}
