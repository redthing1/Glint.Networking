using Glint.Networking.Messages;
using Glint.Networking.Pipeline.Messages;

namespace Glint.Networking.Game.Updates {
    public abstract class GameUpdate {
        public long time;
        public long sourceUid;

        public virtual void copyFrom(GameUpdateMessage msg) {
            time = msg.time;
            sourceUid = msg.sourceUid;
        }
    }
}