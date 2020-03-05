using Glint.Networking.Components;
using Glint.Networking.Messages;
using Glint.Networking.Messages.Types;

namespace Glint.Networking.Game.Updates {
    public abstract class BodyUpdate : GameUpdate {
        public uint bodyId { get; set; }
        public uint syncTag { get; set; }

        public override void copyFrom(GameUpdateMessage msg) {
            base.copyFrom(msg); // call base copy
            var bodyMsg = (BodyUpdateMessage) msg;
            bodyId = bodyMsg.bodyId;
            syncTag = bodyMsg.syncTag;
        }

        public abstract void applyTo(SyncBody body);
    }
}