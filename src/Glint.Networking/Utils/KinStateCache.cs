using Glint.Networking.Game.Updates;
using Glint.Networking.Utils.Collections;

namespace Glint.Networking.Utils {
    public class KinStateCache {
        public ConcurrentRingQueue<StateFrame> stateBuf = new ConcurrentRingQueue<StateFrame>(INTERPOLATION_CACHE_SIZE);
        
        /// <summary>
        /// the number of frames to cache for interpolation (affects latency of rendering updates)
        /// MUST be at least 2
        /// 
        /// </summary>
        public const int INTERPOLATION_CACHE_SIZE = 2;

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