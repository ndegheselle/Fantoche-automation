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

    public class BaseCrudRepository<T> : BaseRepository<T>, ICrudRepository<T> where T : INamed
    {
        public BaseCrudRepository(IMongoDatabase database, string collectionName) : base(database, collectionName)
        { }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _collection.Find(e => e.Id == id).FirstOrDefaultAsync();
        }

        public virtual async Task CreateAsync(T element)
        {
            await _collection.InsertOneAsync(element);
        }

        public virtual async Task UpdateAsync(T element)
        {
            await _collection.ReplaceOneAsync(e => e.Id == element.Id, element);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            var filter = Builders<T>.Filter.Eq(e => e.Id, id);
            await _collection.DeleteOneAsync(e => e.Id == id);
        }
    }
}
