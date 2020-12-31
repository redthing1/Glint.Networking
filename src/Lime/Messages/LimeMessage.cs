using Lidgren.Network;

namespace Lime.Messages {
    public abstract class LimeMessage {
        public byte id { get; set; }
        public NetConnection source { get; set; }
        public abstract NetDeliveryMethod deliveryMethod { get; }
    }
}