using Lidgren.Network;
using Lime.Messages;
using MessagePack;

namespace Glint.Networking.Messages {
    public class HeartbeatMessage : LimeMessage {
        [Key(0)] public bool alive { get; set; }
        public override NetDeliveryMethod deliveryMethod => NetDeliveryMethod.Unreliable;
    }
}