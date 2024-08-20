using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared;
using Automation.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("workflows")]
    public class WorkflowController
    {
        public WorkflowController(IMongoDatabase database)
        {
        }
    }
}
