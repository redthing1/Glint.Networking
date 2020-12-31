using Glint.Networking.Game;
using Lidgren.Network;
using Lime.Messages;
using MessagePack;

namespace Glint.Networking.Messages {
    public class PresenceMessage : LimeMessage {
        [Key(0)] public long myUid { get; set; }
        [Key(1)] public bool here { get; set; }
        [Key(1)] public string myNick { get; set; }

        public override NetDeliveryMethod deliveryMethod => NetDeliveryMethod.ReliableOrdered;

        public void createFrom(NetPlayer peer) {
            myUid = peer.uid;
            myNick = peer.nick;
        }
    }
}