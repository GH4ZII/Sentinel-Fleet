using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SentinelFleet.Application.Telemetry;

namespace SentinelFleet.Infrastructure.Telemetry;

public sealed class TelemetryQueuePublisher : ITelemetryQueuePublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnection _connection;
    private readonly ILogger<TelemetryQueuePublisher> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IChannel? _channel;
    private bool _topologyReady;

    public TelemetryQueuePublisher(IConnection connection, ILogger<TelemetryQueuePublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task PublishAsync(QueuedTelemetryMessage message, CancellationToken cancellationToken = default)
    {
        await EnsureTopologyAsync(cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));
        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = message.EventId
        };

        await _channel!.BasicPublishAsync(
            exchange: TelemetryQueueNames.Exchange,
            routingKey: TelemetryQueueNames.RoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogDebug("Published telemetry event {EventId} for asset {AssetId}", message.EventId, message.AssetId);
    }

    private async Task EnsureTopologyAsync(CancellationToken cancellationToken)
    {
        if (_topologyReady && _channel is { IsOpen: true })
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_topologyReady && _channel is { IsOpen: true })
            {
                return;
            }

            if (_channel is not null)
            {
                await _channel.DisposeAsync();
                _channel = null;
            }

            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await DeclareTopologyAsync(_channel, cancellationToken);
            _topologyReady = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    internal static async Task DeclareTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: TelemetryQueueNames.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: TelemetryQueueNames.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: TelemetryQueueNames.Queue,
            exchange: TelemetryQueueNames.Exchange,
            routingKey: TelemetryQueueNames.RoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        _initLock.Dispose();
    }
}
