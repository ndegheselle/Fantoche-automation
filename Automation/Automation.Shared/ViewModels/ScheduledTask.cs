using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Shared.ViewModels
{
    public enum EnumInstanceState
    {
        Pending,
        Running,
        Completed,
        Failed
    }

    // TODO : Inputs and outputs log
    public class TaskInstance
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public EnumInstanceState State { get; set; }
    }

    public class ScheduledTask
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }

        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? Day { get; set; }
        public int? Hour { get; set; }
        public int? Minute { get; set; }
        public int? DayOfWeek { get; set; }
    }
}
