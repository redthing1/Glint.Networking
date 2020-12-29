using Glint.Networking.Game.Updates;
using Glint.Networking.Utils.Collections;

namespace Glint.Networking.Utils {
    public class KinStateCache {
        public ConcurrentRingQueue<StateFrame> stateBuf = new ConcurrentRingQueue<StateFrame>(CACHE_BUFSIZE);
        private const int CACHE_BUFSIZE = 2;

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