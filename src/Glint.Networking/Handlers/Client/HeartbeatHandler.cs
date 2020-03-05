using Glint.Networking.Game;
using Glint.Networking.Messages;

namespace Glint.Networking.Handlers.Client {
    public class HeartbeatHandler : ClientMessageHandler<HeartbeatMessage> {
        public HeartbeatHandler(GameSyncer syncer) : base(syncer) { }

        public override bool handle(HeartbeatMessage msg) {
            return true; // heartbeats can be silently accepted
        }
    }
}