using Confluent.Kafka;
using FluentResults;
using Newtonsoft.Json;
using static Confluent.Kafka.ConfigPropertyNames;

namespace Paybook.Cycle.Core
{
    //Command
    //Handler
    //Event
    //Producer
    //Consumer
    //Handler
    public interface ICommand
    {
    }

    public record PagamentoCommand : ICommand
    {
        public string FirstName { get; set; }
    }

    public interface IEvent
    {
    }

    public record PagamentoCriadoEvent : IEvent
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
    }

    public interface IHandler<TCommand> where TCommand : ICommand
    {
        Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
    }

    public class PagamentoCommandHandler : IHandler<PagamentoCommand>
    {
        private readonly IProducer<PagamentoCriadoEvent> _producer;

        public PagamentoCommandHandler(IProducer<PagamentoCriadoEvent> command)
        {
            _producer = command;
        }

        public async Task<Result> Handle(PagamentoCommand command, CancellationToken cancellationToken)
        {
            await _producer.Send(new PagamentoCriadoEvent() { Id = "Id", FirstName = "FirstName" }, cancellationToken);
            return Result.Ok();
        }
    }
    public interface IProducer<TEvent> where TEvent : IEvent
    {
        Task<Result> Send(TEvent @event, CancellationToken cancellationToken);
        Task<Result> Flush(CancellationToken cancellationToken);
    }

    public class PagamentoCriadoProducer : IProducer<PagamentoCriadoEvent>, IDisposable
    {
        private readonly IProducer<int, string> _kafkaProducer;

        public PagamentoCriadoProducer()
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
            };
            _kafkaProducer = new ProducerBuilder<int, string>(config).Build();
        }

        public async Task<Result> Send(PagamentoCriadoEvent @event, CancellationToken cancellationToken)
        {
            var topic = "test-topic";
            //var message = MensagemProducerFactory.Create(@event);
            var message = new Message<int, string>() { Key = 1, Value = JsonConvert.SerializeObject(@event) };
            var produzido = await _kafkaProducer.ProduceAsync(topic, message, cancellationToken);

            return produzido.Status == PersistenceStatus.Persisted ? Result.Ok() : Result.Fail($"Cant Produce the Message: {message}");
        }

        public Task<Result> Flush(CancellationToken cancellationToken)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092",
                Acks = Acks.All
            };
            using var producer = new ProducerBuilder<int, string>(config).Build();
            producer.Flush(cancellationToken);

            return Task.FromResult(Result.Ok());
        }

        public void Dispose()
        {
            _kafkaProducer?.Flush();
            _kafkaProducer?.Dispose();
        }
    }
    public interface IConsumer<TEvent> where TEvent : IEvent
    {
        Task<TEvent> Consume(CancellationToken cancellationToken);
    }
    public class PagamentoCriadoConsumer : IConsumer<PagamentoCriadoEvent>, IDisposable
    {
        private readonly IConsumer<int, string> _kafkaConsumer;

        public PagamentoCriadoConsumer()
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "test-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            _kafkaConsumer = new ConsumerBuilder<int, string>(config).Build();
        }

        public Task<PagamentoCriadoEvent> Consume(CancellationToken cancellationToken)
        {
            _kafkaConsumer.Subscribe("test-topic");
            try
            {
                while (true)
                {
                    var @event = _kafkaConsumer.Consume();
                    if (@event != null)
                    {
                        return Task.FromResult(JsonConvert.DeserializeObject<PagamentoCriadoEvent>(@event.Message.Value)!);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw ex;
            }
        }

        public void Dispose()
        {
            _kafkaConsumer?.Dispose();
        }
    }

    public static class MensagemProducerFactory
    {
        public static Message<int, string> Create(IEvent @event)
        {
            var mensagem = JsonConvert.SerializeObject(new MensagemProducer(@event));
            return new Message<int, string>
            {
                Key = 1,
                Value = mensagem
            };
        }
    }
    public record MensagemProducer
    {
        public MensagemProducer(IEvent @event)
        {
            Event = @event;
        }
        public IEvent Event { get; set; }
        public Guid Key { get { return Guid.NewGuid(); } }
    }
}