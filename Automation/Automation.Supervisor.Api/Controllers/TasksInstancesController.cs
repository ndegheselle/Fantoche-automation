using Automation.Dal;
using Automation.Dal.Repositories;
using Automation.Models.Work;
using Microsoft.AspNetCore.Mvc;

namespace Automation.Supervisor.Api.Controllers
{
    [ApiController]
    [Route("instances")]
    public class TasksInstancesController : BaseCrudController<TaskInstance>
    {
        public TasksInstancesController(DatabaseConnection connection) : base(new TaskIntancesRepository(connection))
        {}
    }
}
