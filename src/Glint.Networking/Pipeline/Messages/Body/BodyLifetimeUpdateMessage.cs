using Lidgren.Network;
using MessagePack;

namespace Glint.Networking.Messages {
    public class BodyLifetimeUpdateMessage : BodyUpdateMessage {
        [Key(4)] public bool exists { get; set; }

        public override NetDeliveryMethod deliveryMethod { get; } = NetDeliveryMethod.ReliableOrdered;
    }
}