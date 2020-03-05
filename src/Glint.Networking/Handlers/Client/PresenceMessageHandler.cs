using System.Linq;
using Glint.Networking.Game;
using Glint.Networking.Game.Updates;
using Glint.Networking.Messages;
using Glint.Util;

namespace Glint.Networking.Handlers.Client {
    public class PresenceMessageHandler : ClientMessageHandler<PresenceMessage> {
        public PresenceMessageHandler(GameSyncer syncer) : base(syncer) { }

        public override bool handle(PresenceMessage msg) {
            // my own presence updates connected state
            if (msg.myUid == syncer.uid) {
                if (!syncer.connected && msg.here) {
                    Global.log.writeLine($"confirmed connection to server ({msg.source}",
                        GlintLogger.LogLevel.Information);
                }

                syncer.connected = msg.here;
                syncer.connectionStatusChanged?.Invoke(syncer.connected);
                return true;
            }

            // update peer info
            if (msg.here) {
                var peer = syncer.peers.SingleOrDefault(x => x.uid == msg.myUid);
                if (peer == null) { // create if not exist
                    peer = new GamePeer(msg.myRemId, msg.myUid);
                    syncer.peers.Add(peer);
                }

                // update nick (in case we don't have it)
                if (peer.remId != msg.myRemId) {
                    peer.remId = msg.myRemId;
                    Global.log.writeLine($"updated nickname to {peer.remId} for uid: {peer.uid} from introduction",
                        GlintLogger.LogLevel.Trace);
                }

                Global.log.writeLine($"received hello from {peer}", GlintLogger.LogLevel.Information);
                syncer.connectivityUpdates.Enqueue(new ConnectivityUpdate(peer,
                    ConnectivityUpdate.ConnectionStatus.Connected));
                syncer.gamePeerConnected(peer);
            } else {
                var peer = syncer.peers.Single(x => x.uid == msg.myUid);
                syncer.peers.Remove(peer);
                Global.log.writeLine($"received bye from {peer}", GlintLogger.LogLevel.Information);
                syncer.gamePeerDisconnected(peer);
            }

            return true;
        }
    }
}