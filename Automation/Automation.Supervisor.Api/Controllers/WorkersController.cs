using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Microsoft.AspNetCore.Mvc;

namespace Automation.Supervisor.Api.Controllers
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
