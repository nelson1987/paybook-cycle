using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Paybook.Cycle.Core
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }
    }

    public class Pagamento
    {
        [BsonId]
        public Guid Id { get; set; }
        public string FirstName { get; set; }
    }

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
}