using Fantoche.Dal;
using Fantoche.Dal.Repositories;
using Fantoche.Models.Work;
using Fantoche.Shared.Data.Execution;
using Microsoft.AspNetCore.Mvc;

namespace Fantoche.Supervisor.Api.Controllers
{
    [ApiController]
    [Route("instances")]
    public class TasksInstancesController : BaseCrudController<TaskInstance>
    {
        public TasksInstancesController(DatabaseConnection connection) : base(new TaskInstancesRepository(connection))
        {}
    }
}
