using Lidgren.Network;
using MessagePack;
using scopely.msgpacksharp;

namespace Lime.Messages {
    [MessagePackObject]
    public abstract class LimeMessage {
        [IgnoreMember] public byte id { get; set; }
        [IgnoreMember] public NetConnection source { get; set; }
        [IgnoreMember] public abstract NetDeliveryMethod deliveryMethod { get; }
    }
}