using Lidgren.Network;
using Lime.Messages;
using MsgPack.Serialization;

namespace Glint.Networking.Messages {
    public class HeartbeatMessage : LimeMessage {
        [MessagePackMember(0)] public bool alive { get; set; }
        public override NetDeliveryMethod deliveryMethod => NetDeliveryMethod.Unreliable;
    }
}