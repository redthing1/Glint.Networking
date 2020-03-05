using Lime.Messages;

namespace Glint.Networking.Handlers {
    public abstract class MessageHandler<TMessage> : IMessageHandler where TMessage : LimeMessage {
        public abstract bool handle(TMessage msg);
        public bool handle(LimeMessage msg) => handle((TMessage) msg);
    }
}