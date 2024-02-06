using Confluent.Kafka;
using FluentResults;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Paybook.Cycle.Core;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit.Categories;

namespace Paybook.Cycle.Tests
{
    public partial class Tests
    {
        /// <summary>
        ///     A simple test that produces a couple of messages then
        ///     consumes them back.
        /// </summary>
        [Fact]
        public void SimpleProduceConsume()
        {
            string bootstrapServers = "localhost:9092";
            string singlePartitionTopic = "test-topic";
            // LogToFile("start SimpleProduceConsume");

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            var consumerConfig = new ConsumerConfig
            {
                GroupId = Guid.NewGuid().ToString(),
                BootstrapServers = bootstrapServers,
                //SessionTimeoutMs = 6000,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            string testString1 = "hello world";
            string testString2 = null;

            DeliveryResult<Null, string> produceResult1;
            DeliveryResult<Null, string> produceResult2;
            using (var producer = new ProducerBuilder<Null, string>(producerConfig).Build())
            {
                produceResult1 = ProduceMessage(singlePartitionTopic, producer, testString1);
                //produceResult2 = ProduceMessage(singlePartitionTopic, producer, testString2);
            }

            using (var consumer = new ConsumerBuilder<byte[], byte[]>(consumerConfig).Build())
            {
                ConsumeMessage(consumer, produceResult1, testString1);
                //ConsumeMessage(consumer, produceResult2, testString2);
            }

            Assert.Equal(0, Library.HandleCount);


            //LogToFile("end   SimpleProduceConsume");
        }

        private static void ConsumeMessage(IConsumer<byte[], byte[]> consumer, DeliveryResult<Null, string> dr, string testString)
        {
            //consumer.Assign(new List<TopicPartitionOffset>() { dr.TopicPartitionOffset });
            consumer.Assign(new TopicPartition("test-topic", 1));
            var r = consumer.Consume(TimeSpan.FromSeconds(10));
            Assert.NotNull(r?.Message);
            Assert.Equal(testString, r.Message.Value == null ? null : Encoding.UTF8.GetString(r.Message.Value, 0, r.Message.Value.Length));
            Assert.Null(r.Message.Key);
            Assert.Equal(r.Message.Timestamp.Type, dr.Message.Timestamp.Type);
            Assert.Equal(r.Message.Timestamp.UnixTimestampMs, dr.Message.Timestamp.UnixTimestampMs);
        }

        private static DeliveryResult<Null, string> ProduceMessage(string topic, IProducer<Null, string> producer, string testString)
        {
            var result = producer.ProduceAsync(topic, new Message<Null, string> { Value = testString }).Result;
            Assert.NotNull(result?.Message);
            Assert.Equal(topic, result.Topic);
            Assert.NotEqual<long>(result.Offset, Offset.Unset);
            Assert.Equal(TimestampType.CreateTime, result.Message.Timestamp.Type);
            Assert.True(Math.Abs((DateTime.UtcNow - result.Message.Timestamp.UtcDateTime).TotalMinutes) < 1.0);
            Assert.Equal(0, producer.Flush(TimeSpan.FromSeconds(10)));
            return result;
        }
    }

    public class IntegrationTestsApiFactory : WebApplicationFactory<Program>
    //public class IntegrationTestsApiFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development")
                    .ConfigureTestServices(services =>
                    {
                        services
                        .AddSingleton<IMongoContext, MongoContext>()
                        .AddSingleton<IUnitOfWork, UnitOfWork>()
                        .AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>))
                        .AddSingleton<ICommand, PagamentoCommand>()
                        .AddSingleton<IEvent, PagamentoCriadoEvent>()
                        .AddSingleton<IHandler<PagamentoCommand>, PagamentoCommandHandler>()
                        .AddSingleton<IProducer<PagamentoCriadoEvent>, PagamentoCriadoProducer>()
                        .AddSingleton<IConsumer<PagamentoCriadoEvent>, PagamentoCriadoConsumer>();
                        //service.AddTransient<IPagamentoRepository, PagamentoRepository>();
                    });
        }
        //IConsumer<Null, string> consumer
        public Task Consume<TConsumer>(TimeSpan? timeout = null) where TConsumer : IConsumer<PagamentoCriadoEvent>
        {
            const int defaultTimeoutInSeconds = 1;
            timeout ??= TimeSpan.FromSeconds(defaultTimeoutInSeconds);

            using var scope = Services.CreateScope();
            var consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();
            if (Debugger.IsAttached)
                return consumer.Consume(CancellationToken.None);

            using var tokenSource = new CancellationTokenSource(timeout.Value);
            return consumer.Consume(tokenSource.Token);
        }
    }

    [Collection(nameof(IntegrationApiTestFixtureCollection))]
    public class IntegrationTests : IntegrationTest
    {
        //private readonly IMongoRepository<Pagamento> _caixa;

        public IntegrationTests(IntegrationTestFixture<Program> integrationTestFixture) : base(integrationTestFixture)
        {
            //_caixa = ApiFixture.Factory.Services.GetRequiredService<IMongoRepository<Pagamento>>();
        }

        [Fact]
        public async Task Realiza_Get_Com_Sucesso()
        {
            var response = await ApiFixture.Client.GetAsync("/weatherForecast");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.True(!string.IsNullOrEmpty(result));
        }

        [Fact(DisplayName = "Efetua Saque via api")]
        public async Task Realiza_Post_Com_Sucesso()
        {
            Pagamento pagamentoEsperado = new Pagamento() { FirstName = "Paga" };
            var response = await ApiFixture.Client.PostAsJsonAsync($"/weatherForecast", pagamentoEsperado);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.Contains("Receba seu saque", result);
        }

        [Fact(DisplayName = "NÃO Efetua Saque via api")]
        public async Task Nao_Efetua_Saque_Via_Api()
        {
            var response = await ApiFixture.Client.PostAsJsonAsync($"/weatherForecast", new { });
            //var resposta = await response.Content.ReadAsStringAsync();

            Assert.False(response.IsSuccessStatusCode);
            //Assert.Contains("Valor não válido para saque", resposta);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Teste_de_Repository()
        {
            Pagamento pagamentoEsperado = new Pagamento() { FirstName = "Paga" };
            await RepositoryFixture.Repository.InsertOneAsync(pagamentoEsperado);
            var resultadoCedulas = await RepositoryFixture.Repository.FindByIdAsync(pagamentoEsperado.Id.ToString());
            Assert.Equal(pagamentoEsperado.Id, resultadoCedulas!.Id);
            Assert.Equal(pagamentoEsperado.FirstName, resultadoCedulas!.FirstName);
        }

        [Fact]
        public async Task Teste_de_PubSub()
        {
            PagamentoCriadoEvent pagamentoEsperado = new PagamentoCriadoEvent() { Id = Guid.NewGuid().ToString(), FirstName = "Paga" };
            await KafkaFixture.Producer.Flush(CancellationToken.None);
            var producer = await KafkaFixture.Producer.Send(pagamentoEsperado, CancellationToken.None);
            Assert.True(producer.IsSuccess);
            var resultadoCedulas = await KafkaFixture.Consumer.Consume(CancellationToken.None)!;
            Assert.Equal(pagamentoEsperado.Id, resultadoCedulas.Id);
            Assert.Equal(pagamentoEsperado.FirstName, resultadoCedulas.FirstName);
        }
    }

    [CollectionDefinition(nameof(IntegrationApiTestFixtureCollection))]
    public class IntegrationApiTestFixtureCollection : ICollectionFixture<IntegrationTestFixture<Program>>
    {
    }

    public class IntegrationTestFixture<TStartup> : IDisposable where TStartup : class
    {
        //public readonly IntegrationTestsApiFactory<TStartup> Factory;
        public readonly IntegrationTestsApiFactory Factory;
        public HttpClient Client;

        public IntegrationTestFixture()
        {
            var clientOptions = new WebApplicationFactoryClientOptions()
            {
                HandleCookies = false,
                BaseAddress = new Uri("http://localhost"),
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 7
            };

            //Factory = new IntegrationTestsApiFactory<TStartup>();
            Factory = new IntegrationTestsApiFactory();
            Client = Factory.CreateClient(clientOptions);
        }

        public void Dispose()
        {
            Client.Dispose();
            Factory.Dispose();
        }
    }

    public class RepositoryFixture
    {
        public IMongoRepository<Pagamento> Repository { get; set; }

        //public RepositoryFixture(IntegrationTestsApiFactory<Program> factory)
        public RepositoryFixture(IntegrationTestsApiFactory factory)
        {
            Repository = factory.Services.GetRequiredService<IMongoRepository<Pagamento>>();
        }
    }

    public class KafkaFixture
    {
        public IProducer<PagamentoCriadoEvent> Producer { get; set; }
        public IConsumer<PagamentoCriadoEvent> Consumer { get; set; }

        //public KafkaFixture(IntegrationTestsApiFactory<Program> factory)
        public KafkaFixture(IntegrationTestsApiFactory factory)
        {
            Producer = factory.Services.GetRequiredService<IProducer<PagamentoCriadoEvent>>();
            Consumer = factory.Services.GetRequiredService<IConsumer<PagamentoCriadoEvent>>();
        }
        public Result<T> Consume<T>(string topic, int msTimeout = 150)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "test-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            using var consumer = new ConsumerBuilder<int, string>(config).Build();
            consumer.Subscribe("test-topic");
            try
            {
                while (true)
                {
                    var @event = consumer.Consume();
                    if (@event != null)
                    {
                        return Result.Ok(JsonConvert.DeserializeObject<T>(@event.Message.Value)!);
                        //return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<PagamentoCriadoEvent>(@event.Message.Value)!);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw ex;
            }
        }
    }

    [Collection(nameof(IntegrationApiTestFixtureCollection))]
    [IntegrationTest]
    public partial class IntegrationTest
    {
        public IntegrationTestFixture<Program> ApiFixture;
        public RepositoryFixture RepositoryFixture;
        public KafkaFixture KafkaFixture;

        public IntegrationTest(IntegrationTestFixture<Program> integrationTestFixture)
        {
            ApiFixture = integrationTestFixture;
            RepositoryFixture = new RepositoryFixture(ApiFixture.Factory);
            KafkaFixture = new KafkaFixture(ApiFixture.Factory);
        }

    }
}