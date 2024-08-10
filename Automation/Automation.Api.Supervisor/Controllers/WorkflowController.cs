using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("workflows")]
    public class WorkflowController : BaseCrudController<WorkflowRepository, WorkflowNode>, IWorkflowRepository<WorkflowNode>
    {
        public WorkflowController(IMongoDatabase database) : base(new WorkflowRepository(database))
        {}
    }
}
