using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Modules;
using FrameWork.Interfaces.EventBus;
using FrameWork.Serialization.Protobuf;
using NUnit.Framework;
using ProtoBuf;
using FrameWork.EventBus.Rabbit.Extension.Extensions;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.WaitStrategies;
using RabbitMQ.Client;

namespace FrameWork.EventBus.Rabbit.IntegrationTests;

public class PubSubTests
{

    private TestcontainersContainer? rabbitContainer;

    [OneTimeSetUp]
    public async Task Setup() {
        var testcontainersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
        .WithImage("rabbitmq:3-management")
        .WithName("rabbitmq-integration-test")
        .WithPortBinding(5672)
        .WithPortBinding(15672)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672));

        rabbitContainer = testcontainersBuilder.Build();
        await rabbitContainer.StartAsync();        
    }

    [OneTimeTearDown]
    public async Task TearDown() {
        if (rabbitContainer != null) {
            await rabbitContainer.DisposeAsync();
        }
    }

    [Test]
    public async Task PublishEvent_SameDomain_ReceivesEvent()
    {
        var rabbitOptions = new Action<ConnectionOptions>((opt) => {
            opt.URIs = new List<Uri>() {
                new Uri("amqp://guest:guest@localhost:5672/")
            };
        });

        var publisher = new RabbitEventPublisher("testingnamespace", rabbitOptions, 
            new ProtobufSerializer());
        await publisher.Start();
        publisher.Attach(new BasicRetryObserver<RabbitEventPublisher>());


        var subscriber = new RabbitEventSubscriber("testingnamespace", 
            rabbitOptions, 
            new ProtobufSerializer());

        var eventHandler = new TestSubscriber();
        await subscriber.SubscribeAsync<TestSubscriber, TestEvent>("testingnamespace", eventHandler);
        await subscriber.Start();
        subscriber.Attach(new BasicRetryObserver<RabbitEventSubscriber>());
        
        var expectedEvent = new TestEvent() {
            Id = Guid.NewGuid()
        };

        await publisher.Publish(expectedEvent);

        Thread.Sleep(100);

        Assert.AreEqual(eventHandler.ReceivedEvents.Count, 1);
        Assert.AreEqual(eventHandler.ReceivedEvents[0].Id, expectedEvent.Id);
        await publisher.Stop();
        await subscriber.Stop();
    }

    [Test]
    public async Task PublishEvent_DifferentDomains_ReceivesEvent()
    {
        var rabbitOptions = new Action<ConnectionOptions>((opt) => {
            opt.URIs = new List<Uri>() {
                new Uri("amqp://guest:guest@localhost:5672/")
            };
        });

        var publisher = new RabbitEventPublisher("pubnamespace", rabbitOptions, 
            new ProtobufSerializer());
        publisher.Attach(new BasicRetryObserver<RabbitEventPublisher>());
        await publisher.Start();

        var subscriber = new RabbitEventSubscriber("subnamespace", 
            rabbitOptions, 
            new ProtobufSerializer());
        subscriber.Attach(new BasicRetryObserver<RabbitEventSubscriber>());

        var eventHandler = new TestSubscriber();
        await subscriber.SubscribeAsync<TestSubscriber, TestEvent>("pubnamespace", eventHandler);
        await subscriber.Start();

        var expectedEvent = new TestEvent() {
            Id = Guid.NewGuid()
        };

        await publisher.Publish(expectedEvent);

        Thread.Sleep(100);

        Assert.AreEqual(eventHandler.ReceivedEvents.Count, 1);
        Assert.AreEqual(eventHandler.ReceivedEvents[0].Id, expectedEvent.Id);

        await publisher.Stop();
        await subscriber.Stop();
    }

    [ProtoContract]
    public class TestEvent {
        [ProtoMember(1)]
        public Guid Id {get;set;}
    }

    private class TestSubscriber : IEventHandler<TestEvent>
    {
        public List<TestEvent> ReceivedEvents = new List<TestEvent>();
        public Task<EventHandleResult> Handle(TestEvent @event)
        {
            ReceivedEvents.Add(@event);
            return Task.FromResult(EventHandleResult.Success);
        }
    }
}