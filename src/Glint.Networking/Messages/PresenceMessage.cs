using Glint.Networking.Game;
using Lidgren.Network;
using Lime.Messages;
using MsgPack.Serialization;

namespace Glint.Networking.Messages {
    public class PresenceMessage : LimeMessage {
        [MessagePackMember(0)] public long myUid { get; set; }
        [MessagePackMember(1)] public bool here { get; set; }

        public override NetDeliveryMethod deliveryMethod => NetDeliveryMethod.ReliableOrdered;

        public void createFrom(NetPlayer peer) {
            myUid = peer.uid;
        }
    }
}