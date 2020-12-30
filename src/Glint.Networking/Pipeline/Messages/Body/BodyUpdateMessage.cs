using Glint.Networking.Components;
using Glint.Networking.Pipeline.Messages;
using MsgPack.Serialization;

namespace Glint.Networking.Messages {
    public abstract class BodyUpdateMessage : GameUpdateMessage {
        [MessagePackMember(2)] public uint bodyId { get; set; }
        [MessagePackMember(3)] public uint syncTag { get; set; }

        public virtual void createFrom(SyncBody body) {
            bodyId = body.bodyId;
            syncTag = body.bodyType;
        }
    }
}