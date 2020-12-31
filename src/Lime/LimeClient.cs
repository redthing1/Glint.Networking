using Lidgren.Network;

namespace Lime {
    public class LimeClient : LimeNode {
        public NetClient lidgrenClient => (NetClient) lidgrenPeer;
        public NetConnection serverConn => lidgrenClient.ServerConnection;
        public LimeClient(Configuration config) : base(config, new NetClient(config.peerConfig)) { }

        public NetConnection connect(string host, in int port, string hail) {
            return lidgrenClient.Connect(host, port, lidgrenClient.CreateMessage(hail));
        }

        public void disconnect() {
            lidgrenClient.Disconnect("yeet");
        }
    }
}