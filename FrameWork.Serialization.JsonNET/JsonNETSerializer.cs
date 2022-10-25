using FrameWork.Interfaces;
using Newtonsoft.Json;

namespace FrameWork.Serialization.JsonNET;
public class JsonNETSerializer : ISerializer
{
    private readonly JsonSerializerSettings settings;
    public JsonNETSerializer(JsonSerializerSettings settings)
    {
        this.settings = settings;
    }

    public T Deserialize<T>(string serialized)
    {
        var result = JsonConvert.DeserializeObject<T>(serialized, settings);
        if (result == null)
        {
            throw new Exception("Cannot deserialize this item");
        }
        return result;
    }

    public string Serialize(object obj)
    {
        return JsonConvert.SerializeObject(obj, settings);
    }
}
