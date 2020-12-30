using Glint.Networking.Components;
using Glint.Networking.Messages;
using Glint.Networking.Messages.Types;
using Glint.Networking.Pipeline.Messages;

namespace Glint.Networking.Game.Updates {
    public class BodyKinUpdate : BodyUpdate {
        public PackedVec2 pos { get; set; }
        public PackedVec2 vel { get; set; }
        public float angle { get; set; }
        public float angularVelocity { get; set; }

        public override void copyFrom(GameUpdateMessage msg) {
            base.copyFrom(msg);
            var kinMsg = (BodyKinematicUpdateMessage) msg;
            pos = kinMsg.pos.copy();
            vel = kinMsg.vel.copy();
            angle = kinMsg.angle;
            angularVelocity = kinMsg.angularVelocity;
        }
        
        public override void applyTo(SyncBody body) {
            body.pos = pos.unpack();
            body.velocity = vel.unpack();
            body.angle = angle;
            body.angularVelocity = angularVelocity;
        }
    }
}