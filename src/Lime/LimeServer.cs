using Lidgren.Network;

namespace Lime {
    public class LimeServer : LimeNode {
        public NetServer lidgrenServer => (NetServer) lidgrenPeer;

        public LimeServer(Configuration config) : base(config, new NetServer(config.peerConfig)) { }
    }
}