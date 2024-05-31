using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.App.ViewModels
{
    public class GlobalContext
    {
        // Lazy singleton
        private static readonly Lazy<GlobalContext> _instance = new Lazy<GlobalContext>(() => new GlobalContext());
        public static GlobalContext Instance => _instance.Value;

        private GlobalContext()
        {
            SideMenuVM = new SideMenuVM();
        }

        public SideMenuVM SideMenuVM { get; }
    }
}
