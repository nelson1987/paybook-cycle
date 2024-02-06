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
        public int Id { get; set; }
        public string FirstName { get; set; }
    }

    public interface IPagamentoRepository
    {
        Pagamento GetById(int idPagamento);
    }

    public class PagamentoRepository : IPagamentoRepository
    {
        public Pagamento GetById(int idPagamento)
        {
            return new Pagamento() { Id = 1, FirstName = "Paga" };
        }
    }
}