using Automation.Dal.Repositories;
using Automation.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("tasks")]
    public class TaskController : ControllerBase
    {
        private readonly TaskRepository _taskRepo;
        public TaskController(MongoClient client)
        {
            _taskRepo = new TaskRepository(client);
        }
    }
}
