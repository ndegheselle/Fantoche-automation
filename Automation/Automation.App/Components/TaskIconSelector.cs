using Automation.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components
{
    class TaskIconSelector : DataTemplateSelector
    {
        public DataTemplate ScopeIconTemplate { get; set; }
        public DataTemplate WorkflowIconTemplate { get; set; }
        public DataTemplate TaskIconTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TaskScope)
                return ScopeIconTemplate;
            if (item is TaskWorkflow)
                return WorkflowIconTemplate;
            if (item is ITask)
                return TaskIconTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}
