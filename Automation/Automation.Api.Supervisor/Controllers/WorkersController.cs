using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Realtime;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("workers")]
    public class WorkersController
    {
        private readonly WorkersRealtimeClient _realtime;

        public WorkersController(RedisConnectionManager redis)
        {
            _realtime = new WorkersRealtimeClient(redis.Connection);
        }

        [HttpGet]
        [Route("")]
        public Task<IEnumerable<WorkerInstance>> GetAll()
        {
            return _realtime.GetWorkersAsync();
        }
    }
}
