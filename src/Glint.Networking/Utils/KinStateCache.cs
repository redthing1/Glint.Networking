using Glint.Networking.Game.Updates;
using Glint.Networking.Utils.Collections;

namespace Glint.Networking.Utils {
    public class KinStateCache {
        public ConcurrentRingQueue<StateFrame> stateBuf;

        public KinStateCache(int interpCacheSize) {
            stateBuf = new ConcurrentRingQueue<StateFrame>(interpCacheSize);
        }

        public struct StateFrame {
            public BodyKinUpdate data;
            public long receivedAt;

            public StateFrame(BodyKinUpdate data, long receivedAt) {
                this.data = data;
                this.receivedAt = receivedAt;
            }
        }
    }
}