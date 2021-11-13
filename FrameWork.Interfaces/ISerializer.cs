using System;
namespace FrameWork.Interfaces;

public interface ISerializer : IDeserializer
{
    string Serialize(Object obj);
}

public interface IByteSerializer : IByteDeserializer
{
    byte[]? Serialize(Object obj);
}
