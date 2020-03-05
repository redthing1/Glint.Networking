using Glint.Networking.Components;
using Glint.Networking.Messages.Types;
using MsgPack.Serialization;

namespace Glint.Networking.Messages {
    public class BodyUpdateMessage : GameUpdateMessage {
        [MessagePackMember(2)] public uint bodyId { get; set; }
        [MessagePackMember(3)] public uint syncTag { get; set; }
        [MessagePackMember(4)] public PackedVec2 pos { get; set; }
        [MessagePackMember(5)] public PackedVec2 vel { get; set; }
        [MessagePackMember(6)] public float angle { get; set; }
        [MessagePackMember(7)] public float angularVelocity { get; set; }

        public void createFrom(SyncBody body) {
            bodyId = body.bodyId;
            syncTag = body.syncTag;
            pos = new PackedVec2(body.pos);
            vel = new PackedVec2(body.velocity);
            angle = body.angle;
            angularVelocity = body.angularVelocity;
        }
    }
}