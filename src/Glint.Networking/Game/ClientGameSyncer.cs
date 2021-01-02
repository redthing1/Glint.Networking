using Glint.Networking.Messages;
using Lime;
using Nez;

namespace Glint.Networking.Game {
    public class ClientGameSyncer : GameSyncer {
        public ClientGameSyncer(LimeClient node, string host, int port, string nickname, int netUps, int bodyUps,
            bool debug = false) :
            base(node, host, port, nickname, netUps, bodyUps, debug) { }

        public new LimeClient node => (LimeClient) base.node;

        public void connect() {
            // open connection and go
            Global.log.info($"connecting to server node ({host}:{port})");
            node.start();
            node.connect(host, port, "hail");
            nodeUpdateTimer = Core.Schedule(1f / netUps, true, timer => { node.update(); });
        }

        public void disconnect() {
            Global.log.info($"requesting disconnect from server");
            var intro = node.getMessage<PresenceMessage>();
            intro.myUid = node.uid;
            intro.here = false;
            node.sendToAll(intro);
            node.disconnect();
        }
    }
}