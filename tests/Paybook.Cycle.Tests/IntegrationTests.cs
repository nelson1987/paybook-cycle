using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Paybook.Cycle.Core;
using System.Net;
using System.Net.Http.Json;
using Xunit.Categories;

namespace Paybook.Cycle.Tests
{
    public class IntegrationTestsApiFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development")
                    .ConfigureTestServices(services =>
                    {
                        services.AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>));
                        //service.AddTransient<IPagamentoRepository, PagamentoRepository>();
                    });
        }
    }

    [Collection(nameof(IntegrationApiTestFixtureCollection))]
    public class IntegrationTests : IntegrationTest
    {
        private readonly IMongoRepository<Pagamento> _caixa;
        public IntegrationTests(IntegrationTestFixture<Program> integrationTestFixture) : base(integrationTestFixture)
        {
            _caixa = ApiFixture.Factory.Services.GetRequiredService<IMongoRepository<Pagamento>>();
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
    }


    [CollectionDefinition(nameof(IntegrationApiTestFixtureCollection))]
    public class IntegrationApiTestFixtureCollection : ICollectionFixture<IntegrationTestFixture<Program>>
    {

    }

    public class IntegrationTestFixture<TStartup> : IDisposable where TStartup : class
    {
        public readonly IntegrationTestsApiFactory<TStartup> Factory;
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

            Factory = new IntegrationTestsApiFactory<TStartup>();
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
        public RepositoryFixture(IntegrationTestsApiFactory<Program> factory)
        {
            Repository = factory.Services.GetRequiredService<IMongoRepository<Pagamento>>();
        }
    }

    [Collection(nameof(IntegrationApiTestFixtureCollection))]
    [IntegrationTest]
    public partial class IntegrationTest
    {
        public IntegrationTestFixture<Program> ApiFixture;
        public RepositoryFixture RepositoryFixture;
        public IntegrationTest(IntegrationTestFixture<Program> integrationTestFixture)
        {
            ApiFixture = integrationTestFixture;
            RepositoryFixture = new RepositoryFixture(ApiFixture.Factory);
        }

        //[Fact]
        //public async Task Test1()
        //{
        //    var response = await _integrationTestFixture.Client.GetAsync("/weatherForecast");
        //    var result = await response.Content.ReadAsStringAsync();
        //    Assert.True(!string.IsNullOrEmpty(result));
        //}

        //[Fact(DisplayName = "Efetua Saque via api")]
        //public async Task Efetua_Saque_Via_Api()
        //{
        //    var response = await _integrationTestFixture.Client.PostAsJsonAsync($"/weatherForecast", new { });
        //    response.EnsureSuccessStatusCode();
        //    var result = await response.Content.ReadAsStringAsync();            
        //    Assert.Contains("Receba seu saque", result);
        //}
    }
}
