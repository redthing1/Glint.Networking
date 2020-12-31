using Glint.Networking.Components;
using Glint.Networking.Messages.Types;
using MsgPack.Serialization;

namespace Glint.Networking.Pipeline.Messages {
    public class BodyKinematicUpdateMessage : BodyUpdateMessage {
        [MessagePackMember(4)] public PackedVec2 pos { get; set; }
        [MessagePackMember(5)] public PackedVec2 vel { get; set; }
        [MessagePackMember(6)] public float angle { get; set; }
        [MessagePackMember(7)] public float angularVelocity { get; set; }

        public override void createFrom(SyncBody body) {
            base.createFrom(body);
            pos = new PackedVec2(body.pos);
            vel = new PackedVec2(body.velocity);
            angle = body.angle;
            angularVelocity = body.angularVelocity;
        }
    }
}