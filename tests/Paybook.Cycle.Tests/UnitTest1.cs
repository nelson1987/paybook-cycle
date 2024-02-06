using Microsoft.AspNetCore.Mvc.Testing;

namespace Paybook.Cycle.Tests
{
    public class UnitTest1
    {
        private readonly HttpClient _httpClient;
        public UnitTest1()
        {
            var webApplicationFactory = new WebApplicationFactory<Program>();
            _httpClient = webApplicationFactory.CreateDefaultClient();
        }

        [Fact]
        public async Task Test1()
        {
            var response = await _httpClient.GetAsync("/weatherForecast");
            var result = await response.Content.ReadAsStringAsync();
            Assert.True(!string.IsNullOrEmpty(result));
        }
    }
}
