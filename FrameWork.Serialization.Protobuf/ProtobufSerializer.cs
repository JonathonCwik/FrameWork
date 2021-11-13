using FrameWork.Interfaces;
using ProtoBuf;

namespace FrameWork.Serialization.Protobuf;
public class ProtobufSerializer : IByteSerializer, IByteDeserializer
{
    public T Deserialize<T>(byte[] serialized)
    {
        using (var stream = new MemoryStream(serialized)) {
            return Serializer.Deserialize<T>(stream);  
        }
    }


    public byte[]? Serialize(object record)
    {
        if (null == record) return null;  
  
        using (var stream = new MemoryStream())  
        {  
            Serializer.Serialize(stream, record);  
            return stream.ToArray();  
        }
    }
}
