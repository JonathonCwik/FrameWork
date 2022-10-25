using System;
namespace FrameWork.EventBus.MQTT
{
    public class MqttEventAttribute : Attribute
    {
        public MqttEventAttribute(string topic) {
            Topic = topic;
        }
        public string Topic { get; }
    }
}