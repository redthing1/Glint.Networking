using Glint.Networking.Utils;
using Lidgren.Network;
using Lime.Messages;
using MsgPack.Serialization;

namespace Glint.Networking.Messages {
    public class GameUpdateMessage : LimeMessage {
        [MessagePackMember(0)] public long time { get; set; }
        [MessagePackMember(1)] public uint sourceUid { get; set; }

        public override NetDeliveryMethod deliveryMethod => NetDeliveryMethod.UnreliableSequenced;
        
        public virtual void reset() {
            time = NetworkTime.time();
        }

        public override string ToString() {
            return $"{GetType().Name}(+time: {time - NetworkTime.startTime}, uid: {sourceUid})";
        }
    }
}