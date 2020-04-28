using Glint.Networking.Messages;
using Lime;
using Nez;

namespace Glint.Networking.Game {
    public class ClientGameSyncer : GameSyncer {
        public ClientGameSyncer(LimeClient node, int netUps, int systemUps, int ringBufferSize, bool debug = false) :
            base(node, netUps, systemUps, ringBufferSize, debug) { }

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
            intro.myNick = netNode.lidNick;
            intro.myUid = uid;
            intro.here = false;
            netNode.sendToAll(intro);
            netNode.disconnect();
        }
    }
}