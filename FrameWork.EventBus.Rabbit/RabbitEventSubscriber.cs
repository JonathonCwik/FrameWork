using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FrameWork.Interfaces;
using FrameWork.Interfaces.EventBus;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FrameWork.EventBus.Rabbit;

public class RabbitEventSubscriber : RabbitEventBusBase<RabbitEventSubscriber>
{
    public readonly string Namespace = "";
    private List<IModel> channels = new List<IModel>();
    private readonly List<Tuple<string, Func<IModel, Task<EventingBasicConsumer>>>> handlers = new List<Tuple<string, Func<IModel, Task<EventingBasicConsumer>>>>();

    
    public RabbitEventSubscriber(string _namespace, Action<ConnectionOptions> options,
                                 IByteSerializer serializer) : base(options, serializer)
    {
        this.Namespace = _namespace;
        SetEventBus(this);
    }

    public override async Task Start()
    {
        await base.Start();

        if (conn.IsOpen)
        {
            channels.Clear();

            foreach (var handler in handlers)
            {
                var consumerChannel = conn.CreateModel();
                consumerChannel.BasicConsume(handler.Item1, false, await handler.Item2.Invoke(consumerChannel));
                channels.Add(consumerChannel);
            }
        }
    }

    public async Task SubscribeAsync<TEventHandler, TEvent>(string _eventNamespace, TEventHandler eventHandler)
        where TEventHandler : IEventHandler<TEvent>
        where TEvent : class
    {
        var eventType = typeof(TEvent);
        var eventName = eventType.Name.ToLower();

        var eventHandlerType = typeof(TEventHandler);
        var eventHandlerName = eventHandlerType.Name.ToLower();

        var rabbitMqAttribute = typeof(TEventHandler).GetCustomAttributes(true)
                .FirstOrDefault(e => e.GetType() == typeof(RabbitHandlerAttribute))
            as RabbitHandlerAttribute;

        var exchange = "fw.exch.event." + _eventNamespace;
        var dlx = exchange.Replace("exch", "dlx");
        var queueName = GetQueueName(eventName, eventHandlerName, _eventNamespace);
        var deadletterQueue = queueName.Replace("queue", "deadletter");
        var topic = GetTopic(eventName, _eventNamespace);

        if (rabbitMqAttribute != null && rabbitMqAttribute.DeliverToAllListeners)
        {
            var id = Guid.NewGuid().ToString("N");

            dlx = dlx + "." + id;
            queueName = queueName + "." + id;
            deadletterQueue = deadletterQueue + "." + id;
        }

        async Task<EventingBasicConsumer> Consumer(IModel channel)
        {
            await Task.CompletedTask;
            var basicConsumer = new EventingBasicConsumer(channel);
            var queueArgs = new Dictionary<string, object> { { "x-dead-letter-exchange", dlx } };
            channel.ExchangeDeclare(exchange, "topic", true, false);

            // Setup the DLX
            channel.ExchangeDeclare(dlx, "topic", false,
                rabbitMqAttribute != null && rabbitMqAttribute.DeliverToAllListeners);

            // Setup Queue for DLX
            channel.QueueDeclare(deadletterQueue, false,
                rabbitMqAttribute != null && rabbitMqAttribute.DeliverToAllListeners,
                rabbitMqAttribute != null && rabbitMqAttribute.DeliverToAllListeners);

            // Setup Main Queue
            channel.QueueDeclare(queueName, true, false,
                rabbitMqAttribute != null && rabbitMqAttribute.DeliverToAllListeners, queueArgs);

            channel.QueueBind(deadletterQueue, dlx, topic);
            channel.QueueBind(queueName, exchange, topic);

            basicConsumer.Received += async (sender, arguments) =>
            {
                TEvent @event;

                try
                {
                    @event = Serializer.Deserialize<TEvent>(arguments.Body);
                }
                catch (Exception)
                {
                    channel.BasicNack(arguments.DeliveryTag, false, false);                
                    throw;
                }

                try
                {
                    var result = await eventHandler.Handle(@event);

                    if (result == EventHandleResult.Success) {
                        channel.BasicAck(arguments.DeliveryTag, false);
                    } else if (result == EventHandleResult.ExpectedFailure) {
                        channel.BasicAck(arguments.DeliveryTag, false);
                    }
                }
                catch (Exception)
                {
                    if (arguments.Redelivered)
                    {
                        return;
                    }
                    channel.BasicReject(arguments.DeliveryTag, true);
                }
            };

            return basicConsumer;
        }

        if (State == EventBusState.Connected)
        {
            var channel = conn.CreateModel();
            if (rabbitMqAttribute != null && rabbitMqAttribute.PrefetchCount > 0)
            {
                channel.BasicQos(0, rabbitMqAttribute.PrefetchCount, false);
            }
            channel.BasicConsume(queueName, false, await Consumer(channel));
            channels.Add(channel);
            handlers.Add(
                new Tuple<string, Func<IModel, Task<EventingBasicConsumer>>>(queueName, Consumer));
        }
        else
        {
            handlers.Add(
                new Tuple<string, Func<IModel, Task<EventingBasicConsumer>>>(queueName, Consumer));
        }

        await Task.CompletedTask;
    }

    private string GetQueueName(string eventName, string eventHandlerName, string eventDomain)
    {
        return Namespace + ".event." + eventDomain + "." + eventName.ToLower() + "." + eventHandlerName.ToLower() + "." + "queue";
    }

    private string GetTopic(string eventName, string domain)
    {
        return domain.ToLower() + ".event." + eventName.ToLower();
    }
}
