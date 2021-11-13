using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using FrameWork.Interfaces;
using FrameWork.Interfaces.EventBus;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace FrameWork.EventBus.Rabbit;

public class RabbitEventPublisher : RabbitEventBusBase<RabbitEventPublisher>
{
    public string ExchangeName { get; private set; }
    private string dlxNameBase;
    public readonly string Namespace;
    private IModel channel;

    public RabbitEventPublisher(string _namespace,
                          Action<ConnectionOptions> options,
                          IByteSerializer serializer) : base(options, serializer)
    {
        ExchangeName = "fw.exch.event." + _namespace;
        dlxNameBase = "fw.exch.event." + _namespace + ".dlx";
        Namespace = _namespace;
        SetEventBus(this);
    }

    public async Task Publish<TEvent>(TEvent @event)
    {
        if (State != EventBusState.Connected && !IsConnOpen()) {
            throw new Exception("Publisher is not connected, cannot publish");
        }

        var eventName = @event.GetType().Name;

        var payload = Serializer.Serialize(@event);

        var props = channel.CreateBasicProperties();
        props.Persistent = true;

        //todo: use Polly and give more than 1 retry with exponential decay
        try
        {
            channel.BasicPublish(ExchangeName, 
                GetTopic(eventName, Namespace), false, props,
                payload);
        }
        catch (AlreadyClosedException e)
        {
            await ReconnectAndRetryPublish(e);
        }
        catch (SocketException e)
        {
            await ReconnectAndRetryPublish(e);
        }

        async Task ReconnectAndRetryPublish(Exception e)
        {
            await Stop();
            await Start();
            channel.BasicPublish(ExchangeName, 
                GetTopic(eventName, Namespace), false, props,
                payload);
        }
    }

    public override async Task Start()
    {
        await base.Start();
        channel = conn.CreateModel();
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        channel.ExchangeDeclare(ExchangeName, "topic", props.Persistent);
    }
    public override async Task Stop()
    {
        await base.Stop();
        channel.Dispose();
    }

    private string GetTopic(string eventName, string domain)
    {
        return domain.ToLower() + ".event." + eventName.ToLower();
    }
}
