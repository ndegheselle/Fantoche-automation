using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Microsoft.AspNetCore.Mvc;

namespace Automation.Api.Supervisor.Controllers
{
    [ApiController]
    [Route("workers")]
    public class WorkersController
    {
        private readonly WorkerRealtimeClient _realtime;

        public WorkersController(RedisConnectionManager connectionManager)
        {
            _realtime = new WorkerRealtimeClient(connectionManager);
        }

        [HttpGet]
        [Route("")]
        public Task<IEnumerable<WorkerInstance>> GetAll()
        {
            return _realtime.GetWorkersAsync();
        }
    }
}
