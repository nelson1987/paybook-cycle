using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Paybook.Cycle.Core;
using System.Net.Http.Json;

namespace Paybook.Cycle.Tests
{
    public class IntegrationTestsApiFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        public ServiceProvider ServiceProvider { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            //builder.UseStartup<TStartup>();
            builder.UseEnvironment("Development")
                    .ConfigureTestServices(service => {
                        service.AddTransient<IPagamentoRepository, PagamentoRepository>();
                        ServiceProvider = service.BuildServiceProvider();
                    });
        }
    }

    [Collection(nameof(IntegrationApiTestFixtureCollection))]
    public class IntegrationTests : IntegrationTest
    {
        private readonly IPagamentoRepository _caixa;
        public IntegrationTests(IntegrationTestFixture<Program> integrationTestFixture) : base(integrationTestFixture)
        {
            _caixa = ApiFixture.Factory.ServiceProvider.GetRequiredService<IPagamentoRepository>();
        }

        [Fact]
        public async Task Test1()
        {
            var response = await ApiFixture.Client.GetAsync("/weatherForecast");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.True(!string.IsNullOrEmpty(result));
        }

        [Fact]
        public void SaqueContemMenorNumeroDeCedulas()
        {
            Pagamento pagamentoEsperado = new Pagamento() { Id = 1, FirstName = "Paga" };
            var resultadoCedulas = RepositoryFixture.Repository.GetById(pagamentoEsperado.Id);
            Assert.Equal(pagamentoEsperado.Id, resultadoCedulas.Id);
            Assert.Equal(pagamentoEsperado.FirstName, resultadoCedulas.FirstName);
        }

        [Fact(DisplayName = "Efetua Saque via api")]
        public async Task Efetua_Saque_Via_Api()
        {
            var response = await ApiFixture.Client.PostAsJsonAsync($"/weatherForecast", new { });
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.Contains("Receba seu saque", result);
        }

        //[Theory(DisplayName = "NÃO Efetua Saque via api")]
        //[InlineData(5)]
        //[InlineData(15)]
        //[InlineData(38)]
        //public async Task Nao_Efetua_Saque_Via_Api(int valorSaque)
        //{
        //    var requisicao = await _integrationTestFixture.Client.PostAsJsonAsync($"/api/CaixaEletronico/saque/{valorSaque}", new { });
        //    var resposta = await requisicao.Content.ReadAsStringAsync();

        //    Assert.False(requisicao.IsSuccessStatusCode);
        //    Assert.Contains("Valor não válido para saque", resposta);
        //    Assert.Equal(HttpStatusCode.BadRequest, requisicao.StatusCode);
        //}
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
        public IPagamentoRepository Repository { get; set; }
        public RepositoryFixture(IntegrationTestsApiFactory<Program> factory)
        {
            Repository = factory.ServiceProvider.GetRequiredService<IPagamentoRepository>();
        }
    }

    [Collection(nameof(IntegrationApiTestFixtureCollection))]
    public class IntegrationTest
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
