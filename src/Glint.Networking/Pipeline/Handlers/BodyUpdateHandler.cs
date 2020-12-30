using Glint.Networking.Game;
using Glint.Networking.Game.Updates;
using Glint.Networking.Messages;

namespace Glint.Networking.Pipeline.Handlers {
    public abstract class BodyUpdateHandler<TMessage> : ClientMessageHandler<TMessage> where TMessage : BodyUpdateMessage {
        public BodyUpdateHandler(GameSyncer syncer) : base(syncer) { }
    }

    public class BodyKinematicUpdateHandler : BodyUpdateHandler<BodyKinematicUpdateMessage> {
        public BodyKinematicUpdateHandler(GameSyncer syncer) : base(syncer) { }
        public override bool handle(BodyKinematicUpdateMessage msg) {
            // we always clone the message because we are pooling instances
            var update = new BodyKinUpdate();
            update.copyFrom(msg);

            syncer.incomingBodyUpdates.Enqueue(update);
            return true;
        }
    }

    public class BodyLifetimeUpdateHandler : BodyUpdateHandler<BodyLifetimeUpdateMessage> {
        public BodyLifetimeUpdateHandler(GameSyncer syncer) : base(syncer) { }
        public override bool handle(BodyLifetimeUpdateMessage msg) {
            // we always clone the message because we are pooling instances
            var update = new BodyLifetimeUpdate();
            update.copyFrom(msg);

            syncer.incomingBodyUpdates.Enqueue(update);
            return true;
        }
    }
}