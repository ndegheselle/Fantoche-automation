using Fantoche.Realtime;
using Fantoche.Realtime.Clients;
using Fantoche.Realtime.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fantoche.Supervisor.Api.Controllers
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
