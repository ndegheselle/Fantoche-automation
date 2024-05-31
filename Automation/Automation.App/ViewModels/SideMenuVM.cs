using Automation.Base;
using Automation.Plugins.Flow;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.App.ViewModels
{
    public class SideMenuVM
    {
        public TaskScope RootScope { get; set; }

        public SideMenuVM()
        {
            RootScope = new TaskScope();
            RootScope.Childrens.Add(new TaskScope()
            {
                Name = "Scope 1",
                Childrens = new List<IContextElement>()
                {
                    new WaitAllTasks() { Name = "Wait all"},
                    new WaitDelay() { Name = "Delay" },
                }
            });
        }
    }
}
