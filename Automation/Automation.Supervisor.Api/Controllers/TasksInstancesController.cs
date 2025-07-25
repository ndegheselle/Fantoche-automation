using Automation.Dal;
using Automation.Dal.Models;
using Automation.Dal.Repositories;
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
