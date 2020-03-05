using Glint.Networking.Components;
using Glint.Networking.Messages;
using Glint.Networking.Messages.Types;

namespace Glint.Networking.Game.Updates {
    public class BodyUpdate : GameUpdate {
        public uint bodyId { get; set; }
        public uint syncTag { get; set; }
        public PackedVec2 pos { get; set; }
        public PackedVec2 vel { get; set; }
        public float angle { get; set; }
        public float angularVelocity { get; set; }

        public void copyFrom(BodyUpdateMessage msg) {
            copyFrom((GameUpdateMessage) msg); // call base copy
            bodyId = msg.bodyId;
            syncTag = msg.syncTag;
            pos = msg.pos.copy();
            vel = msg.vel.copy();
            angle = msg.angle;
            angularVelocity = msg.angularVelocity;
        }
        
        public void applyTo(SyncBody body) {
            body.pos = pos.unpack();
            body.velocity = vel.unpack();
            body.angle = angle;
            body.angularVelocity = angularVelocity;
        }
    }
}