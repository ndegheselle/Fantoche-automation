using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Automation.Server.Shared;
using Automation.Shared.Base;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;
using System.Text.Json;

namespace Automation.Supervisor.Api.Controllers
{
    [ApiController]
    [Route("tasks")]
    public class TasksController : Controller
    {
        private readonly TasksRepository _taskRepo;
        private readonly TaskIntancesRepository _taskInstanceRepo;
        private readonly IMongoDatabase _database;
        private readonly WorkerAssignator _assignator;

        public TasksController(IMongoDatabase database, RedisConnectionManager redis)
        {
            _database = database;
            _taskRepo = new TasksRepository(database);
            _taskInstanceRepo = new TaskIntancesRepository(database);
            _assignator = new WorkerAssignator(database, redis);
        }

        [HttpPost]
        [Route("")]
        public async Task<ActionResult<Guid>> CreateAsync(TaskNode element)
        {
            if (element.ParentId == null)
            {
                return BadRequest(new Dictionary<string, string[]>()
                {
                    {nameof(TaskNode.Name), [$"A task cannot be created without a parent."] }
                });
            }

            var scopeRepository = new ScopesRepository(_database);
            var existingChild = await scopeRepository.GetDirectChildByNameAsync(element.ParentId, element.Name);
            
            if (existingChild != null)
            {
                return BadRequest(new Dictionary<string, string[]>()
                {
                    {nameof(TaskNode.Name), [$"The name {element.Name} is already used in this scope."] }
                });
            }

            var scope = await scopeRepository.GetByIdAsync(element.ParentId.Value);
            if (scope == null)
            {
                return BadRequest(new Dictionary<string, string[]>()
                {
                    {nameof(TaskNode.Name), [$"The parent id {element.ParentId} is invalid."] }
                });
            }

            element.ParentTree = [.. scope.ParentTree, scope.Id];
            return await _taskRepo.CreateAsync(element);
        }

        [HttpDelete]
        [Route("{id}")]
        public Task DeleteAsync([FromRoute] Guid id)
        {
            return _taskRepo.DeleteAsync(id);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<TaskNode?> GetByIdAsync([FromRoute] Guid id)
        {
            return await _taskRepo.GetByIdAsync(id);
        }

        [HttpPut]
        [Route("{id}")]
        public Task UpdateAsync([FromRoute] Guid id, [FromBody]TaskNode element)
        {
            return _taskRepo.UpdateAsync(id, element);
        }

        [HttpPost]
        [Route("{id}/execute")]
        public async Task<TaskInstance> ExecuteAsync([FromRoute] Guid id, [FromBody] string? settings)
        {
            TaskNode? task = await _taskRepo.GetByIdAsync(id);
            if (task == null)
                throw new InvalidOperationException($"No task node found for the id '{id}'.");
            if (task.Package == null)
                throw new InvalidOperationException($"The task '{id}' doesn't have an assigned package.");

            return await _assignator.AssignAsync(task, settings);
        }

        [HttpGet]
        [Route("{id}/instances")]
        public async Task<ListPageWrapper<TaskInstance>> GetInstancesAsync([FromRoute] Guid id, [FromQuery] int page, [FromQuery] int pageSize)
        {
            return await _taskInstanceRepo.GetByTaskAsync(id, page, pageSize);
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
