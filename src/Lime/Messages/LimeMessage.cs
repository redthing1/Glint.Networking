using Lidgren.Network;
using scopely.msgpacksharp;

namespace Lime.Messages {
    public abstract class LimeMessage {
        public byte id { get; set; }
        public NetConnection source { get; set; }
        public abstract NetDeliveryMethod deliveryMethod { get; }

        public void write(NetOutgoingMessage packet) {
            packet.Write(id);
            packet.Write(MsgPackSerializer.SerializeObject(this));
        }
    }
}