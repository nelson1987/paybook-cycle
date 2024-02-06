using Microsoft.AspNetCore.Mvc;
using Paybook.Cycle.Core;

namespace Paybook.Cycle.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IPagamentoRepository _repository;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IPagamentoRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpPost(Name = "PostWeatherForecast")]
        public async Task<string> Post(Pagamento pagamento, CancellationToken cancellationToken)
        {
            await _repository.CreateAsync(pagamento, cancellationToken);
            return "Receba seu saque";
        }
    }
}