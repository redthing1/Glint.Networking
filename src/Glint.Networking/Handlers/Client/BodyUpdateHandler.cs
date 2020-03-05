using Glint.Networking.Game;
using Glint.Networking.Game.Updates;
using Glint.Networking.Messages;
using Glint.Util;

namespace Glint.Networking.Handlers.Client {
    public class BodyUpdateHandler : ClientMessageHandler<BodyUpdateMessage> {
        public BodyUpdateHandler(GameSyncer syncer) : base(syncer) { }

        public override bool handle(BodyUpdateMessage msg) {
            // we always clone the message because we are pooling instances
            var update = default(BodyUpdate);
            switch (msg) {
                case BodyKinematicUpdateMessage kinMsg:
                    update = new BodyKinUpdate();
                    break;
                default:
                    Global.log.writeLine(
                        $"failed to create message queue clone of unmatched message type {msg.GetType()}",
                        GlintLogger.LogLevel.Error);
                    return false; // unable to handle (unrecognized)
            }

            update.copyFrom(msg);

            syncer.bodyUpdates.enqueue(update);
            return true;
        }
    }
}