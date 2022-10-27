using System.Threading;
using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.WaitStrategies;
using FrameWork.Serialization.JsonNET;
using NUnit.Framework;
using FrameWork.Interfaces.EventBus;
using System.Collections.Generic;

namespace FrameWork.EventBus.MQTT.IntegrationTests;

public class PubSubTests
{
    private TestcontainersContainer? mosquittoNoAuthContainer;
    private TestcontainersContainer? mosquittoAuthContainer;

    [OneTimeTearDown]
    public async Task TearDown() {
        if (mosquittoAuthContainer != null) {
            await mosquittoAuthContainer.DisposeAsync();
        }

        if (mosquittoNoAuthContainer != null) {
            await mosquittoNoAuthContainer.DisposeAsync();
        }
    }

    [Test]
    public async Task PublishEvent_NoAuthTwoSubscribers_BothSubscribersReceiveEvent()
    {
        var testcontainersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
        .WithImage("eclipse-mosquitto:latest")
        .WithName("eclipse-mosquitto-with-auth")
        .WithMount("../../../mosquitto-auth.conf", "/mosquitto/config/mosquitto.conf")
        .WithMount("../../../password_file", "/etc/mosquitto/password_file")
        .WithPortBinding("1883", true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1883));

        mosquittoNoAuthContainer = testcontainersBuilder.Build();
        await mosquittoNoAuthContainer.StartAsync();

        var subscriber1 = new MqttEventSubscriber("mqtt://localhost:" + mosquittoNoAuthContainer.GetMappedPublicPort(1883), 
            new JsonNETSerializer(new Newtonsoft.Json.JsonSerializerSettings()), "admin", "password");

        await subscriber1.Start();
        var testEventHandler1 = new TestEventHandler1();
        await subscriber1.SubscribeAsync<TestEventHandler1, TestEvent1>("testing", testEventHandler1);

         var subscriber2 = new MqttEventSubscriber("mqtt://localhost:" + mosquittoNoAuthContainer.GetMappedPublicPort(1883), 
            new JsonNETSerializer(new Newtonsoft.Json.JsonSerializerSettings()), "admin", "password");

        await subscriber2.Start();
        var testEventHandler2 = new TestEventHandler2();
        await subscriber2.SubscribeAsync<TestEventHandler2, TestEvent1>("testing", testEventHandler2);

        var publisher = new MqttEventPublisher("mqtt://localhost:" + mosquittoNoAuthContainer.GetMappedPublicPort(1883), 
            new JsonNETSerializer(new Newtonsoft.Json.JsonSerializerSettings()), "testing", "admin", "password");
        await publisher.Start();

        var testEvent1 = new TestEvent1() {
            Prop1 = Guid.NewGuid().ToString()
        };

        await publisher.Publish(testEvent1);

        Thread.Sleep(1000);
        Assert.That(testEventHandler1.ReceivedEvents.Count, Is.EqualTo(1));
        Assert.That(testEventHandler1.ReceivedEvents[0].Prop1, Is.EqualTo(testEvent1.Prop1));

        Assert.That(testEventHandler2.ReceivedEvents.Count, Is.EqualTo(1));
        Assert.That(testEventHandler2.ReceivedEvents[0].Prop1, Is.EqualTo(testEvent1.Prop1));

        var testEvent2 = new TestEvent1() {
            Prop1 = Guid.NewGuid().ToString()
        };

        await publisher.Publish(testEvent2);

        Thread.Sleep(1000);

        Assert.That(testEventHandler1.ReceivedEvents.Count, Is.EqualTo(2));
        Assert.That(testEventHandler1.ReceivedEvents[1].Prop1, Is.EqualTo(testEvent2.Prop1));

        Assert.That(testEventHandler2.ReceivedEvents.Count, Is.EqualTo(2));
        Assert.That(testEventHandler2.ReceivedEvents[1].Prop1, Is.EqualTo(testEvent2.Prop1));
    }

    [MqttEvent("testEvent1")]
    public class TestEvent1 {
        public string Prop1 { get; set; }
    }

    public class TestEventHandler1 : IEventHandler<TestEvent1>
    {
        public List<TestEvent1> ReceivedEvents = new List<TestEvent1>();
        public Task<EventHandleResult> Handle(TestEvent1 @event)
        {
            ReceivedEvents.Add(@event);
            return Task.FromResult(EventHandleResult.Success);
        }
    }

    public class TestEventHandler2 : IEventHandler<TestEvent1>
    {
        public List<TestEvent1> ReceivedEvents = new List<TestEvent1>();
        public Task<EventHandleResult> Handle(TestEvent1 @event)
        {
            ReceivedEvents.Add(@event);
            return Task.FromResult(EventHandleResult.Success);
        }
    }
}