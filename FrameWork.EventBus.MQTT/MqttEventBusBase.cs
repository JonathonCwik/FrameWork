using System;
using System.Threading.Tasks;
using FrameWork.Interfaces;
using FrameWork.Interfaces.EventBus;
using MQTTnet;
using MQTTnet.Client;

namespace FrameWork.EventBus.MQTT
{
    public class MqttEventBusBase : IBusBase
    {
        private readonly string uri;
        protected EventBusState eventBusState = EventBusState.NotStarted;
        protected IMqttClient mqttClient;
        protected readonly ISerializer serializer;
        private readonly string username;
        private readonly string password;

        public MqttEventBusBase(string uri, ISerializer serializer, string username = null, string password = null)
        {
            this.serializer = serializer;
            this.username = username;
            this.password = password;
            this.uri = uri;
        }

        public EventBusState GetState()
        {
            return eventBusState;
        }

                public async Task Start()
        {
            var mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithConnectionUri(this.uri)
                .WithKeepAlivePeriod(TimeSpan.FromMinutes(5))
                .WithTimeout(TimeSpan.FromSeconds(5))
                .WithClientId(Guid.NewGuid().ToString())
                .WithCleanSession();

            if (!string.IsNullOrWhiteSpace(this.username) && !string.IsNullOrWhiteSpace(this.password)) {
                mqttClientOptions = mqttClientOptions.WithCredentials(username, password);
            }

            var connResult = await mqttClient.ConnectAsync(mqttClientOptions.Build());
            if (connResult.ResultCode == MqttClientConnectResultCode.Success)
            {
                eventBusState = EventBusState.Connected;
            }
            else
            {
                eventBusState = EventBusState.StartedNotConnected;
                throw new System.Exception($"Error connecting to MQTT server ({Enum.GetName(typeof(MqttClientConnectResultCode), connResult.ResultCode)}): {connResult.ReasonString} ");
            }
        }

        public async Task Stop()
        {
            await mqttClient.DisconnectAsync();
            mqttClient.Dispose();
            mqttClient = null;
        }
    }
}