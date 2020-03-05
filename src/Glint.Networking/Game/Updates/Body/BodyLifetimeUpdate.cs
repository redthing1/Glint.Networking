using Glint.Networking.Components;
using Glint.Networking.Messages;

namespace Glint.Networking.Game.Updates {
    public class BodyLifetimeUpdate : BodyUpdate {
        public bool exists { get; set; }

        public override void copyFrom(GameUpdateMessage msg) {
            base.copyFrom(msg);
            var lifMsg = (BodyLifetimeUpdateMessage) msg;
            exists = lifMsg.exists;
        }

        public override void applyTo(SyncBody body) {
            if (!exists) {
                // call destroy on the body
                body.Entity.Destroy();
            }
        }
    }
}