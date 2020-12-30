using Lidgren.Network;
using MsgPack.Serialization;

namespace Glint.Networking.Messages {
    public class BodyLifetimeUpdateMessage : BodyUpdateMessage {
        [MessagePackMember(4)] public bool exists { get; set; }

        public override NetDeliveryMethod deliveryMethod { get; } = NetDeliveryMethod.ReliableOrdered;
    }
}