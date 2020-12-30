using Glint.Networking.Game;
using Glint.Networking.Game.Updates;
using Glint.Networking.Messages;

namespace Glint.Networking.Pipeline.Handlers {
    public abstract class BodyUpdateHandler : ClientMessageHandler<BodyUpdateMessage> {
        public BodyUpdateHandler(GameSyncer syncer) : base(syncer) { }

        public override bool handle(BodyUpdateMessage msg) {
            // we always clone the message because we are pooling instances
            var update = default(BodyUpdate);
            // TODO: instance pooling of body updates
            switch (msg) {
                case BodyKinematicUpdateMessage kinMsg:
                    update = new BodyKinUpdate();
                    break;
                case BodyLifetimeUpdateMessage lifMsg:
                    update = new BodyLifetimeUpdate();
                    break;
                default:
                    Global.log.err(
                        $"failed to create message queue clone of unmatched message type {msg.GetType()}");
                    return false; // unable to handle (unrecognized)
            }

            update.copyFrom(msg);

            syncer.bodyUpdates.enqueue(update);
            return true;
        }
    }

    public class BodyKinematicUpdateHandler : BodyUpdateHandler {
        public BodyKinematicUpdateHandler(GameSyncer syncer) : base(syncer) { }
    }

    public class BodyLifetimeUpdateHandler : BodyUpdateHandler {
        public BodyLifetimeUpdateHandler(GameSyncer syncer) : base(syncer) { }
    }
}