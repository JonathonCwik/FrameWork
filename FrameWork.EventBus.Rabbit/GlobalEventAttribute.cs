using System;

namespace FrameWork.EventBus.Rabbit;

public class GlobalEventAttribute : Attribute
{
    public string Domain { get; set; }
}
