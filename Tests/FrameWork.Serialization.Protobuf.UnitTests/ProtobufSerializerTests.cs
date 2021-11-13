using System;
using NUnit.Framework;
using FrameWork.Serialization.Protobuf;
using ProtoBuf;
using ParkSquare.Testing.Generators;

namespace FrameWork.Serialization.Protobuf.UnitTests;

public class ProtobufSerializerTests
{
    [Test]
    public void SerializeAndDeserialize_ProtoContractModel_AllFieldsReturned()
    {
        var serializer = new ProtobufSerializer();

        var model = new TestModel() {
            Id = 1,
            Name = NameGenerator.AnyName(),
            Child = new ChildModel() {
                Id = Guid.NewGuid(),
                Name = NameGenerator.AnyTitle(),
                DateTime = DateTimeGenerator.AnyDate()
            },
            AGuid = Guid.NewGuid(),
            NotAProtoMember = NameGenerator.AnyForename()
        };

        var serialized = serializer.Serialize(model);

        if (serialized == null) {
            Assert.Fail("Serialized should not be null");
            return;
        }

        var deserialized = serializer.Deserialize<TestModel>(serialized);

        Assert.That(model.Id == deserialized.Id, "Ids do not match");
        Assert.That(model.Name == deserialized.Name, "Names do not match");

        if (deserialized.Child == null) {
            Assert.Fail("Child should not be null");
            return;
        }

        Assert.That(model.Child.Id == deserialized.Child.Id, "Child Ids do not match");
        Assert.That(model.Child.Name == deserialized.Child.Name, "Child Names do not match");
        Assert.That(model.Child.DateTime == deserialized.Child.DateTime, "Child DateTimes do not match");
        Assert.That(model.AGuid == deserialized.AGuid, "AGuids do not match");
        Assert.IsNull(deserialized.NotAProtoMember, "NotAProtoMember should be null");
    }

    [ProtoContract]
    public class TestModel {
        [ProtoMember(1)]
        public int Id {get;set;}
        [ProtoMember(2)]
        public string? Name {get;set;}
        [ProtoMember(3)]
        public ChildModel? Child {get;set;}
        [ProtoMember(4)]
        public Guid AGuid {get;set;}
        public string? NotAProtoMember {get;set;}
    }

    [ProtoContract]
    public class ChildModel {
        [ProtoMember(1)]
        public Guid Id {get;set;}
        [ProtoMember(2)]
        public string? Name {get;set;}
        [ProtoMember(3)]
        public DateTime DateTime {get;set;}
    }
}