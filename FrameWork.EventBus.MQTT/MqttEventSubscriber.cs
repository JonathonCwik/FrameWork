using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameWork.Interfaces;
using FrameWork.Interfaces.EventBus;
using MQTTnet;

namespace FrameWork.EventBus.MQTT
{
    public class MqttEventSubscriber : MqttEventBusBase, IEventSubscriber
    {
        public MqttEventSubscriber(string uri, ISerializer serializer, string username = null, string password = null) : 
            base(uri, serializer, username, password)
        {

        }

        public async Task SubscribeAsync<TEventHandler, TEvent>(string _namespace, TEventHandler eventHandler)
            where TEventHandler : IEventHandler<TEvent>
            where TEvent : class
        {
            var mqttFactory = new MqttFactory();

            var eventAttr = typeof(TEvent).GetCustomAttributes(true)
                .FirstOrDefault(e => e.GetType() == typeof(MqttEventAttribute)) as MqttEventAttribute;

            if (eventAttr == null || eventAttr == default(MqttEventAttribute) || string.IsNullOrWhiteSpace(eventAttr.Topic))
            {
                throw new Exception("Cannot publish without an MqttEventAttribute on the event class and a valid (non-whitespace) topic provided");
            }

            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic(_namespace + "/" + eventAttr.Topic);
                    })
                .Build();

            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                Console.WriteLine("Received application message.");
                var msg = this.serializer.Deserialize<TEvent>(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
                eventHandler.Handle(msg);
                return Task.CompletedTask;
            };

            mqttClient.SubscribeAsync(mqttSubscribeOptions);
        }
    }
}