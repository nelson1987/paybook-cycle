using Confluent.Kafka;
using FluentResults;
using Microsoft.Extensions.Logging;
using static MongoDB.Driver.WriteConcern;

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
            await _producer.Send(new PagamentoCriadoEvent(), cancellationToken);
            return Result.Ok();
        }
    }
    public interface IProducer<TEvent> where TEvent : IEvent
    {
        Task<Result> Send(TEvent @event, CancellationToken cancellationToken);
    }

    public class PagamentoCriadoProducer : IProducer<PagamentoCriadoEvent>
    {
        public async Task<Result> Send(PagamentoCriadoEvent @event, CancellationToken cancellationToken)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
            };
            using var producer = new ProducerBuilder<Null, string>(config).Build();

            var topic = "test-topic";
            var message = MensagemProducerFactory.Create(@event);
            var produzido = await producer.ProduceAsync(topic, message, cancellationToken);

            return produzido.Status == PersistenceStatus.Persisted ? Result.Ok() : Result.Fail($"Cant Produce the Message: {message}");
        }
    }
    public interface IConsumer<TEvent> where TEvent : IEvent
    {
        Task<TEvent> Consume(CancellationToken cancellationToken);
    }
    public class PagamentoCriadoConsumer : IConsumer<PagamentoCriadoEvent>
    {
        public Task<PagamentoCriadoEvent> Consume(CancellationToken cancellationToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "test-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe("test-topic");
            try
            {
                while (true)
                {
                    var message = consumer.Consume();
                    Console.WriteLine($"Received message: {message.Value}");
                    break;
                }
                return Task.FromResult(new PagamentoCriadoEvent() { Id = "", FirstName = "" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw ex;
            }
        }
    }

    public static class MensagemProducerFactory
    {
        public static Message<Null, string> Create(IEvent @event)
        {
            return new Message<Null, string> { Value = System.Text.Json.JsonSerializer.Serialize(new MensagemProducer() { Value = @event }) };
        }
    }
    public record MensagemProducer
    {
        public required IEvent Value { get; init; }
        public Guid Key { get { return Guid.NewGuid(); } }
    }
}