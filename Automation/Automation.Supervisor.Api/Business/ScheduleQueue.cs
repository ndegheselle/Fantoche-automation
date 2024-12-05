using Automation.Dal.Repositories;
using Automation.Shared.Data;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace Automation.Supervisor.Api.Business
{
    public class ScheduleQueue
    {
        public ConcurrentQueue<TaskSchedule> Schedules { get; set; } = [];
        private readonly TasksRepository _taskRepo;

        public ScheduleQueue(IMongoDatabase database)
        {
            _taskRepo = new TasksRepository(database);
            Reload();
        }

        public async void Reload()
        {
            Schedules = new ConcurrentQueue<TaskSchedule>(await _taskRepo.GetScheduled());
        }
        // Remove
        // Update
        // Add
    }
}
