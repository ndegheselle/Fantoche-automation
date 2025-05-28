using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class BaseRepository<T>
    {
        protected readonly IMongoDatabase _database;
        protected readonly IMongoCollection<T> _collection;

        public BaseRepository(IMongoDatabase database, string collectionName)
        {
            _database = database;
            _collection = _database.GetCollection<T>(collectionName);
        }

        public virtual async Task<List<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }
    }

    // TODO : use soft deletion and history (userId, createdAt, updatedAt, deletedAt)
    public class BaseCrudRepository<T> : BaseRepository<T> where T : IIdentifier
    {
        public BaseCrudRepository(IMongoDatabase database, string collectionName) : base(database, collectionName)
        { }

        public virtual async Task<T> GetByIdAsync(Guid id)
        {
            return await _collection.Find(e => e.Id == id).FirstOrDefaultAsync() ?? throw new Exception($"Unknow element with id '{id}'");
        }

        public virtual async Task<IEnumerable<T>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            return await _collection.Find(x => ids.Contains(x.Id)).ToListAsync();
        }

        public virtual async Task<Guid> CreateAsync(T element)
        {
            element.Id = Guid.NewGuid();
            await _collection.InsertOneAsync(element);
            return element.Id;
        }

        public virtual async Task<Guid> CreateIfDoesntExistAsync(T element)
        {
            var existingElement = await _collection.Find(e => e.Id == element.Id).FirstOrDefaultAsync();
            if (existingElement == null)
            {
                await _collection.InsertOneAsync(element);
            }
            return element.Id;
        }

        public virtual async Task UpdateAsync(Guid id, T element)
        {
            await _collection.ReplaceOneAsync(e => e.Id == id, element);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            var filter = Builders<T>.Filter.Eq(e => e.Id, id);
            await _collection.DeleteOneAsync(e => e.Id == id);
        }
    }
}
