using Automation.Dal;
using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;
using Automation.Realtime.Clients;
using Automation.Shared.Base;
using Automation.Shared.Data;
using Automation.Worker.Executor;
using Automation.Worker.Packages;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Globalization;
using System.Text.Json;

namespace Automation.Supervisor.Api.Controllers
{
    [ApiController]
    [Route("tasks")]
    public class TasksController : BaseCrudController<BaseAutomationTask>
    {
        private TasksRepository _taskRepo => (TasksRepository)_crudRepository;
        private readonly TaskIntancesRepository _taskInstanceRepo;
        private readonly DatabaseConnection _connection;
        private readonly RemoteTaskExecutor _executor;
        private readonly IPackageManagement _packageManagement;

        public TasksController(DatabaseConnection connection, RealtimeClients realtimeClients, IPackageManagement packageManagement) : base(new TasksRepository(connection))
        {
            _connection = connection;
            _taskInstanceRepo = new TaskIntancesRepository(_connection);
            _executor = new RemoteTaskExecutor(_connection, realtimeClients);
            _packageManagement = packageManagement;
        }

        [HttpPost]
        [Route("")]
        public override async Task<ActionResult<Guid>> CreateAsync(BaseAutomationTask element)
        {
            if (element.ParentId == null)
            {
                return BadRequest(new Dictionary<string, string[]>()
                {
                    {nameof(ScopedMetadata.Name), [$"A task cannot be created without a parent."] }
                });
            }

            var scopeRepository = new ScopesRepository(_connection);
            var existingChild = await scopeRepository.GetDirectChildByNameAsync(element.ParentId, element.Metadata.Name);
            
            if (existingChild != null)
            {
                return BadRequest(new Dictionary<string, string[]>()
                {
                    {nameof(ScopedMetadata.Name), [$"The name {element.Metadata.Name} is already used in this scope."] }
                });
            }

            var scope = await scopeRepository.GetByIdAsync(element.ParentId.Value);
            if (scope == null)
            {
                return BadRequest(new Dictionary<string, string[]>()
                {
                    {nameof(ScopedMetadata.Name), [$"The parent id {element.ParentId} is invalid."] }
                });
            }

            element.ParentTree = [.. scope.ParentTree, scope.Id];
            return await _taskRepo.CreateAsync(element);
        }

        [HttpPut]
        [Route("{id}")]
        public override async Task UpdateAsync([FromRoute] Guid id, [FromBody] BaseAutomationTask element)
        {
            var task = await _taskRepo.GetByIdAsync(id);
            if (task?.Metadata.IsReadOnly == true)
                throw new InvalidOperationException($"The task '{task.Metadata.Name}' is read-only and cannot be updated.");

            if (task is AutomationTask existingTask && element is AutomationTask updatedTask && existingTask.Target != updatedTask.Target)
            {
                await OnTaskTargetChange(updatedTask);
            }

            await base.UpdateAsync(id, element);
        }

        [HttpPost]
        [Route("{id}/execute")]
        public async Task<TaskInstance> ExecuteAsync([FromRoute] Guid id, [FromBody] TaskParameters parameters)
        {
            TaskInstance instance = new TaskInstance(id, new TaskParameters("", ""));
            return await _executor.ExecuteAsync(instance);
        }

        [HttpGet]
        [Route("{id}/instances")]
        public async Task<ListPageWrapper<TaskInstance>> GetInstancesAsync([FromRoute] Guid id, [FromQuery] int page, [FromQuery] int pageSize)
        {
            return await _taskInstanceRepo.GetByTaskAsync(id, page, pageSize);
        }

        [HttpGet]
        [Route("tags")]
        public Task<IEnumerable<string>> GetTagsAsync()
        {
            return _taskRepo.GetTagsAsync();
        }

        [HttpGet]
        [Route("tags/{tag}")]
        public Task<IEnumerable<BaseAutomationTask>> GetByTagAsync([FromRoute] string tag)
        {
            return _taskRepo.GetByTagAsync(tag);
        }

        /// <summary>
        /// Special behavior then a task target is updated
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        private async Task OnTaskTargetChange(AutomationTask task)
        {
            if (task.Target is PackageClassTarget target)
            {
                // Update the inputs outputs based on the package
                ITask instance = await _packageManagement.CreateTaskInstanceAsync(target);
                task.Inputs = instance.Inputs;
                task.Outputs = instance.Outputs;

                // TODO : should do something with all the workflow that use this task
            }
        }
    }

    public static class BsonExtensions
    {
        // Rewritten from this answer https://stackoverflow.com/a/71175724 by https://stackoverflow.com/users/7818969/peebo
        // To https://stackoverflow.com/questions/62080252/convert-newtosoft-jobject-directly-to-bsondocument
        public static BsonDocument ToBsonDocument(this JsonElement e, bool writeRootArrayAsDocument = false, bool tryParseDateTimes = false) =>
            e.ValueKind switch
            {
                JsonValueKind.Object =>
                    new(e.EnumerateObject().Select(p => new BsonElement(p.Name, p.Value.ToBsonValue(tryParseDateTimes)))),
                // Newtonsoft converts arrays to documents by using the index as a key, so optionally do the same thing.
                JsonValueKind.Array when writeRootArrayAsDocument =>
                    new(e.EnumerateArray().Select((v, i) => new BsonElement(i.ToString(NumberFormatInfo.InvariantInfo), v.ToBsonValue(tryParseDateTimes)))),
                _ => throw new NotSupportedException($"ToBsonDocument: {e}"),
            };

        public static BsonValue ToBsonValue(this JsonElement e, bool tryParseDateTimes = false) =>
            e.ValueKind switch
            {
                // TODO: determine whether you want strings that look like dates & times to be serialized as DateTime, DateTimeOffset, or just strings.
                JsonValueKind.String when tryParseDateTimes && e.TryGetDateTime(out var v) => BsonValue.Create(v),
                JsonValueKind.String => BsonValue.Create(e.GetString()),
                // TODO: decide whether to convert to Int64 unconditionally, or only when the value is larger than Int32
                JsonValueKind.Number when e.TryGetInt32(out var v) => BsonValue.Create(v),
                JsonValueKind.Number when e.TryGetInt64(out var v) => BsonValue.Create(v),
                // TODO: decide whether to convert floating values to decimal by default.  Decimal has more precision but a smaller range.
                //JsonValueKind.Number when e.TryGetDecimal(out var v) => BsonValue.Create(v),
                JsonValueKind.Number when e.TryGetDouble(out var v) => BsonValue.Create(v),
                JsonValueKind.Null => BsonValue.Create(null),
                JsonValueKind.True => BsonValue.Create(true),
                JsonValueKind.False => BsonValue.Create(false),
                JsonValueKind.Array => new BsonArray(e.EnumerateArray().Select(v => v.ToBsonValue(tryParseDateTimes))),
                JsonValueKind.Object => e.ToBsonDocument(false, tryParseDateTimes),
                _ => throw new NotSupportedException($"ToBsonValue: {e}"),
            };
    }
}
