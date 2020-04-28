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
                    Global.log.info($"confirmed connection to server ({msg.source}");
                }

                syncer.connected = msg.here;
                syncer.connectionStatusChanged?.Invoke(syncer.connected);
                return true;
            }

            // update peer info
            if (msg.here) {
                var peer = syncer.peers.SingleOrDefault(x => x.uid == msg.myUid);
                if (peer == null) { // create if not exist
                    peer = new NetPlayer(msg.myUid);
                    syncer.peers.Add(peer);
                }

                // update nick (in case we don't have it)
                if (peer.uid != msg.myUid) {
                    peer.uid = msg.myUid;
                    Global.log.trace($"updated nickname to {peer.uid} for {nameof(peer.uid)}: {peer.uid} from introduction");
                }

                Global.log.info($"received hello from {peer}");
                syncer.connectivityUpdates.Enqueue(new ConnectivityUpdate(peer,
                    ConnectivityUpdate.ConnectionStatus.Connected));
                syncer.gamePeerConnected(peer);
            } else {
                var peer = syncer.peers.Single(x => x.uid == msg.myUid);
                syncer.peers.Remove(peer);
                Global.log.info($"received bye from {peer}");
                syncer.gamePeerDisconnected(peer);
            }

            return true;
        }
    }
}