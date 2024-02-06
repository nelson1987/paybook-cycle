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
        private readonly IMongoRepository<Pagamento> _repository;
        private readonly IHandler<PagamentoCommand> _handler;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
            IMongoRepository<Pagamento> repository,
            IHandler<PagamentoCommand> handler)
        {
            _logger = logger;
            _repository = repository;
            _handler = handler;
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
            await _repository.InsertOneAsync(pagamento);
            await _handler.Handle(new PagamentoCommand() { }, cancellationToken);
            return "Receba seu saque";
        }
    }
}