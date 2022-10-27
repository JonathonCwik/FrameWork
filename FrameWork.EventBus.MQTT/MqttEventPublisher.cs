using System;
using System.Threading.Tasks;
using FrameWork.Interfaces.EventBus;
using MQTTnet;
using MQTTnet.Client;
using System.Linq;
using FrameWork.Interfaces;

namespace FrameWork.EventBus.MQTT
{
    public class MqttEventPublisher : MqttEventBusBase, IEventPublisher
    {
        private readonly string _namespace;

        public MqttEventPublisher(string uri, ISerializer serializer, string _namespace, string username = null, string password = null) : 
            base(uri, serializer, username, password)
        {
            this._namespace = _namespace;
        }

        public async Task Publish<TEvent>(TEvent @event)
        {
            if (this.eventBusState != EventBusState.Connected)
            {
                throw new Exception("Not able to publish until eventbus is successfully connected");
            }

            await Publish(@event, 0);
        }

        private async Task Publish<TEvent>(TEvent @event, int retry = 0) {
            var eventAttr = typeof(TEvent).GetCustomAttributes(true)
                .FirstOrDefault(e => e.GetType() == typeof(MqttEventAttribute)) as MqttEventAttribute;

            if (eventAttr == null || eventAttr == default(MqttEventAttribute) || string.IsNullOrWhiteSpace(eventAttr.Topic))
            {
                throw new Exception("Cannot publish without an MqttEventAttribute on the event class and a valid (non-whitespace) topic provided");
            }

            var publishResult = await mqttClient.PublishStringAsync(_namespace + "/" + eventAttr.Topic, serializer.Serialize(@event));
            if (publishResult.ReasonCode != MqttClientPublishReasonCode.Success) {
                if (retry > 2) {
                    throw new System.Exception($"Error publishing to MQTT server ({Enum.GetName(typeof(MqttClientConnectResultCode), publishResult.ReasonCode)}): {publishResult.ReasonString} ");
                } else {
                    Console.WriteLine($"WARN: Error publishing to MQTT server ({Enum.GetName(typeof(MqttClientConnectResultCode), publishResult.ReasonCode)}): {publishResult.ReasonString}");
                    if (retry == 0) {
                        await Stop();
                        await Start();
                    }
                    await Task.Delay(retry * 1000);
                    retry++;
                    await Publish(@event, retry);
                }
            }
        }
    }
}