using Glint.Networking.Game;
using Lime.Messages;

namespace Glint.Networking.Pipeline {
    public abstract class ClientMessageHandler<TMessage> : MessageHandler<TMessage> where TMessage : LimeMessage {
        protected GameSyncer syncer;

        public ClientMessageHandler(GameSyncer syncer) {
            this.syncer = syncer;
        }
    }
}