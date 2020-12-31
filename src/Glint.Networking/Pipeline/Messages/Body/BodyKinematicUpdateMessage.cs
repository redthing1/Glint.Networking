using Glint.Networking.Components;
using Glint.Networking.Messages.Types;
using MessagePack;

namespace Glint.Networking.Messages {
    public class BodyKinematicUpdateMessage : BodyUpdateMessage {
        [Key(4)] public PackedVec2 pos { get; set; }
        [Key(5)] public PackedVec2 vel { get; set; }
        [Key(6)] public float angle { get; set; }
        [Key(7)] public float angularVelocity { get; set; }

        public override void createFrom(SyncBody body) {
            base.createFrom(body);
            pos = new PackedVec2(body.pos);
            vel = new PackedVec2(body.velocity);
            angle = body.angle;
            angularVelocity = body.angularVelocity;
        }
    }
}