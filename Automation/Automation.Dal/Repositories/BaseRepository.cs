using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class BaseRepository<T>
    {
        protected readonly DatabaseConnection _connection;
        protected readonly IMongoCollection<T> _collection;

        public BaseRepository(DatabaseConnection connection, string collectionName)
        {
            _connection = connection;
            _collection = _connection.Database.GetCollection<T>(collectionName);
        }

        public virtual async Task<List<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }
    }

    // TODO : use soft deletion and history (userId, createdAt, updatedAt, deletedAt)
    public class BaseCrudRepository<T> : BaseRepository<T> where T : IIdentifier
    {
        public BaseCrudRepository(DatabaseConnection connection, string collectionName) : base(connection, collectionName)
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

        public virtual async Task<Guid> CreateOrUpdateAsync(T element)
        {
            if (element.Id == Guid.Empty)
            {
                element.Id = Guid.NewGuid();
            }

            var options = new ReplaceOptions { IsUpsert = true };
            await _collection.ReplaceOneAsync(e => e.Id == element.Id, element, options);
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
