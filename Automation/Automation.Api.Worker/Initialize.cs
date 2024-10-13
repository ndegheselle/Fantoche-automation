using Automation.Realtime;
using Automation.Realtime.Clients;
using RestSharp;

namespace Automation.Api.Worker
{
    public static class Initialize
    {
        public static async void RegisterWorker(IServiceProvider services)
        {
            RedisConnectionManager redis = services.GetRequiredService<RedisConnectionManager>();
            WorkerRealtimeClient client = new WorkerRealtimeClient(redis);

            // TODO : load config from environement
            await client.AddWorkerAsync(new Realtime.Models.WorkerInstance()
            {
                Id = Guid.NewGuid().ToString(),
            });
        }
    }
}
