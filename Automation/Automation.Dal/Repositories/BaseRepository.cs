using Automation.Dal.Models;
using Automation.Shared;
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
    }

    public class BaseCrudRepository<T> : BaseRepository<T>, ICrudClient<T> where T : INamed
    {
        public BaseCrudRepository(IMongoDatabase database, string collectionName) : base(database, collectionName)
        { }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _collection.Find(e => e.Id == id).FirstOrDefaultAsync();
        }

        public virtual async Task<IEnumerable<T>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            return await _collection.Find(x => ids.Contains(x.Id)).ToListAsync();
        }

        public virtual async Task<Guid> CreateAsync(T element)
        {
            element.Id = Guid.NewGuid();
            await _collection.InsertOneAsync(element);
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
