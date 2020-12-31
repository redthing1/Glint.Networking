using Glint.Networking.Utils;
using Lidgren.Network;
using Lime.Messages;
using MessagePack;

namespace Glint.Networking.Pipeline.Messages {
    public abstract class GameUpdateMessage : LimeMessage {
        [Key(0)] public long time { get; set; }
        [Key(1)] public long sourceUid { get; set; }

        public override NetDeliveryMethod deliveryMethod => NetDeliveryMethod.UnreliableSequenced;
        
        public virtual void reset() {
            time = NetworkTime.time();
        }

        public override string ToString() {
            return $"{GetType().Name}(+time: {time - NetworkTime.startTime}, uid: {sourceUid})";
        }
    }
}