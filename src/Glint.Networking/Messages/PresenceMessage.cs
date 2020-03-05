using Glint.Networking.Game;
using Lidgren.Network;
using Lime.Messages;
using MsgPack.Serialization;

namespace Glint.Networking.Messages {
    public class PresenceMessage : LimeMessage {
        [MessagePackMember(0)] public uint myUid { get; set; }
        [MessagePackMember(1)] public long myRemId { get; set; }
        [MessagePackMember(2)] public bool here { get; set; }

        public override NetDeliveryMethod deliveryMethod => NetDeliveryMethod.ReliableOrdered;

        public void createFrom(GamePeer peer) {
            myUid = peer.uid;
            myRemId = peer.remId;
        }
    }
}