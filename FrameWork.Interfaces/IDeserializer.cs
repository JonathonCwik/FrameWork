namespace FrameWork.Interfaces;

public interface IDeserializer
{
    T Deserialize<T>(String serialized);
}

public interface IByteDeserializer
{
    T Deserialize<T>(byte[] serialized);
}
