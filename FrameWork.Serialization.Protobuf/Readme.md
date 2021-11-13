# FrameWork.Serialization.Protobuf

## Purpose

FrameWork.Serialization.Protobuf is an implementation of IByteSerializer using `protobuf-net`.

## Usage

### Create Contract
```
    [ProtoContract]
    public class TestModel {
        [ProtoMember(1)]
        public int Id {get;set;}
        [ProtoMember(2)]
        public string? Name {get;set;}
    }
```

### Serialize and Deserialize
```
var serializer = new ProtobufSerializer();
var model = new TestModel() {
    Id = 1,
    Name = NameGenerator.AnyName()
};
var serialized = serializer.Serialize(model);
...
var deserialized = serializer.Deserialize<TestModel>(serialized);
```
