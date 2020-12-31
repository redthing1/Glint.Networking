using Glint.Networking.Components;
using Glint.Networking.Pipeline.Messages;
using MessagePack;

namespace Glint.Networking.Messages {
    public abstract class BodyUpdateMessage : GameUpdateMessage {
        [Key(2)] public uint bodyId { get; set; }
        [Key(3)] public uint syncTag { get; set; }

        public virtual void createFrom(SyncBody body) {
            bodyId = body.bodyId;
            syncTag = body.bodyType;
        }
    }
}