using Glint.Networking.Game;
using Glint.Networking.Game.Updates;
using Glint.Networking.Messages;

namespace Glint.Networking.Handlers.Client {
    public class BodyUpdateHandler : ClientMessageHandler<BodyUpdateMessage> {
        public BodyUpdateHandler(GameSyncer syncer) : base(syncer) { }

        public override bool handle(BodyUpdateMessage msg) {
            // we always clone the message because we are pooling instances
            var update = new BodyUpdate();
            update.copyFrom(msg);
            syncer.bodyUpdates.enqueue(update);
            return true;
        }
    }
}