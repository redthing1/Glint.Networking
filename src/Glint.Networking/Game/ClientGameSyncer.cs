using Glint.Networking.Messages;
using Lime;
using Nez;

namespace Glint.Networking.Game {
    public class ClientGameSyncer : GameSyncer {
        public ClientGameSyncer(LimeClient node, string host, int port, string nickname, int netUps, int systemUps, int ringBufferSize, bool debug = false) :
            base(node, host, port, nickname, netUps, systemUps, ringBufferSize, debug) { }

        public new LimeClient netNode => (LimeClient) base.netNode;

        public void connect() {
            // open connection and go
            Global.log.info($"connecting to server node ({host}:{port})");
            netNode.start();
            netNode.connect(host, port, "hail");
            nodeUpdateTimer = Core.Schedule(1f / netUps, true, timer => { netNode.update(); });
        }

        public void disconnect() {
            Global.log.info($"requesting disconnect from server");
            var intro = netNode.getMessage<PresenceMessage>();
            intro.myUid = netNode.uid;
            intro.here = false;
            netNode.sendToAll(intro);
            netNode.disconnect();
        }
    }
}