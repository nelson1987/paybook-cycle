using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace Paybook.Cycle.Core
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }
    }
    public interface IEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        ObjectId Id { get; set; }

        DateTime CreatedAt { get; }
    }
    public abstract class Document : IEntity
    {
        public ObjectId Id { get; set; }

        public DateTime CreatedAt => Id.CreationTime;
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BsonCollectionAttribute : Attribute
    {
        public string CollectionName { get; }

        public BsonCollectionAttribute(string collectionName)
        {
            CollectionName = collectionName;
        }
    }

    [BsonCollection("Pagamento")]
    public class Pagamento : Document
    {
        public string FirstName { get; set; }
    }
    public interface IMongoRepository<TDocument> where TDocument : IEntity 
    {
        IQueryable<TDocument> AsQueryable();

        IEnumerable<TDocument> FilterBy(
            Expression<Func<TDocument, bool>> filterExpression);

        IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression,
            Expression<Func<TDocument, TProjected>> projectionExpression);

        TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression);

        Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression);

        TDocument FindById(string id);

        Task<TDocument> FindByIdAsync(string id);

        void InsertOne(TDocument document);

        Task InsertOneAsync(TDocument document);

        void InsertMany(ICollection<TDocument> documents);

        Task InsertManyAsync(ICollection<TDocument> documents);

        void ReplaceOne(TDocument document);

        Task ReplaceOneAsync(TDocument document);

        void DeleteOne(Expression<Func<TDocument, bool>> filterExpression);

        Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression);

        void DeleteById(string id);

        Task DeleteByIdAsync(string id);

        void DeleteMany(Expression<Func<TDocument, bool>> filterExpression);

        Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression);
    }
    
    public class MongoRepository<TDocument> : IMongoRepository<TDocument>
    where TDocument : IEntity
    {
        private readonly IMongoCollection<TDocument> _collection;

        public MongoRepository()
        {
            var database = new MongoClient("mongodb://root:example@localhost:27017/").GetDatabase("sales");
            _collection = database.GetCollection<TDocument>(GetCollectionName(typeof(TDocument)));
        }

        private protected string GetCollectionName(Type documentType)
        {
            return ((BsonCollectionAttribute)documentType.GetCustomAttributes(
                    typeof(BsonCollectionAttribute),
                    true)
                .FirstOrDefault())?.CollectionName;
        }

        public virtual IQueryable<TDocument> AsQueryable()
        {
            return _collection.AsQueryable();
        }

        public virtual IEnumerable<TDocument> FilterBy(
            Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.Find(filterExpression).ToEnumerable();
        }

        public virtual IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression,
            Expression<Func<TDocument, TProjected>> projectionExpression)
        {
            return _collection.Find(filterExpression).Project(projectionExpression).ToEnumerable();
        }

        public virtual TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.Find(filterExpression).FirstOrDefault();
        }

        public virtual Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() => _collection.Find(filterExpression).FirstOrDefaultAsync());
        }

        public virtual TDocument FindById(string id)
        {
            var objectId = new ObjectId(id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            return _collection.Find(filter).SingleOrDefault();
        }

        public virtual Task<TDocument> FindByIdAsync(string id)
        {
            return Task.Run(() =>
            {
                var objectId = new ObjectId(id);
                var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
                return _collection.Find(filter).SingleOrDefaultAsync();
            });
        }


        public virtual void InsertOne(TDocument document)
        {
            _collection.InsertOne(document);
        }

        public virtual Task InsertOneAsync(TDocument document)
        {
            return Task.Run(() => _collection.InsertOneAsync(document));
        }

        public void InsertMany(ICollection<TDocument> documents)
        {
            _collection.InsertMany(documents);
        }


        public virtual async Task InsertManyAsync(ICollection<TDocument> documents)
        {
            await _collection.InsertManyAsync(documents);
        }

        public void ReplaceOne(TDocument document)
        {
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
            _collection.FindOneAndReplace(filter, document);
        }

        public virtual async Task ReplaceOneAsync(TDocument document)
        {
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
            await _collection.FindOneAndReplaceAsync(filter, document);
        }

        public void DeleteOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            _collection.FindOneAndDelete(filterExpression);
        }

        public Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() => _collection.FindOneAndDeleteAsync(filterExpression));
        }

        public void DeleteById(string id)
        {
            var objectId = new ObjectId(id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            _collection.FindOneAndDelete(filter);
        }

        public Task DeleteByIdAsync(string id)
        {
            return Task.Run(() =>
            {
                var objectId = new ObjectId(id);
                var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
                _collection.FindOneAndDeleteAsync(filter);
            });
        }

        public void DeleteMany(Expression<Func<TDocument, bool>> filterExpression)
        {
            _collection.DeleteMany(filterExpression);
        }

        public Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Task.Run(() => _collection.DeleteManyAsync(filterExpression));
        }
    }
    /*
    public interface IPagamentoRepository
    {
        Task<Pagamento?> GetAsync(Guid id, CancellationToken cancellationToken = default);
        Task CreateAsync(Pagamento newBook, CancellationToken cancellationToken = default);
    }
    public class PagamentoRepository : IPagamentoRepository
    {
        private readonly IMongoCollection<Pagamento> _booksCollection;
        public PagamentoRepository()
        {
            var mongoClient = new MongoClient("mongodb://root:example@localhost:27017/");
            var mongoDatabase = mongoClient.GetDatabase("sales");
            _booksCollection = mongoDatabase.GetCollection<Pagamento>(nameof(Pagamento));
        }

        public async Task<List<Pagamento>> GetAsync(CancellationToken cancellationToken = default) =>
            await _booksCollection.Find(_ => true).ToListAsync(cancellationToken);

        public async Task<Pagamento?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            await _booksCollection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

        public async Task CreateAsync(Pagamento newBook, CancellationToken cancellationToken = default) =>
            await _booksCollection.InsertOneAsync(newBook, cancellationToken);

        public async Task UpdateAsync(Guid id, Pagamento updatedBook, CancellationToken cancellationToken = default) =>
            await _booksCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

        public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default) =>
            await _booksCollection.DeleteOneAsync(x => x.Id == id, cancellationToken);
    }
    */
}