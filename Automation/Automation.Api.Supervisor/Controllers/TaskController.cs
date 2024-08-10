using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("tasks")]
    public class TaskController : BaseCrudController<TaskRepository, TaskNode>, ITaskRepository<TaskNode>
    {
        public TaskController(IMongoDatabase database) : base(new TaskRepository(database))
        {}
    }
}
