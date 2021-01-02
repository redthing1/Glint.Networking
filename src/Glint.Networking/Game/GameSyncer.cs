using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Glint.Networking.Components;
using Glint.Networking.Game.Updates;
using Glint.Networking.Messages;
using Glint.Networking.Pipeline;
using Glint.Networking.Pipeline.Handlers;
using Glint.Networking.Pipeline.Messages;
using Lidgren.Network;
using Lime;
using Lime.Messages;
using Nez;

namespace Glint.Networking.Game {
    /// <summary>
    /// connects to a game server and fill message queues with data
    /// </summary>
    public abstract class GameSyncer : IDisposable {
        /// <summary>
        /// the underlying networking node
        /// </summary>
        public LimeNode node { get; }

        /// <summary>
        /// updates per second for sending network packets
        /// </summary>
        public int netUps { get; }

        /// <summary>
        /// updates per second for snapshotting synced bodies
        /// </summary>
        public int bodyUps { get; }

        public List<NetPlayer> peers { get; } = new List<NetPlayer>();
        public bool connected;
        public Action<bool> connectionStatusChanged;
        public MessageHandlerContainer handlers { get; } = new MessageHandlerContainer();
        public Action<NetPlayer> gamePeerConnected;
        public Action<NetPlayer> gamePeerDisconnected;

        // message queues
        /// <summary>
        /// incoming connectivity updates
        /// </summary>
        public ConcurrentQueue<ConnectivityUpdate> connectivityUpdates { get; } =
            new ConcurrentQueue<ConnectivityUpdate>();

        /// <summary>
        /// incoming body updates
        /// </summary>
        public ConcurrentQueue<BodyUpdate> incomingBodyUpdates { get; }

        /// <summary>
        /// outgoing game updates
        /// </summary>
        public ConcurrentQueue<GameUpdateMessage> outgoingGameUpdates { get; }

        protected ITimer nodeUpdateTimer;

        // - properties
        public long uid => node.uid;

        /// <summary>
        /// the hostname to connect to
        /// </summary>
        public string host { get; }

        /// <summary>
        /// the port to connect to
        /// </summary>
        public int port { get; }

        /// <summary>
        /// a human-friendly player nickname to identify the current player
        /// </summary>
        public string nickname { get; }
#if DEBUG
        public bool debug { get; }
#endif

        public GameSyncer(LimeNode node, string host, int port, string nickname, int netUps, int bodyUps,
            bool debug = false) {
            this.node = node;
            this.host = host;
            this.port = port;
            this.nickname = nickname;
            this.netUps = netUps;
            this.bodyUps = bodyUps;
#if DEBUG
            this.debug = debug;
#endif

            this.node.configureGlint();
            this.node.initialize();

            incomingBodyUpdates = new ConcurrentQueue<BodyUpdate>();
            outgoingGameUpdates = new ConcurrentQueue<GameUpdateMessage>();

            // wire events
            this.node.onPeerConnected += onPeerConnected;
            this.node.onPeerDisconnected += onPeerDisconnected;
            this.node.onMessage += onMessage;
            this.node.onUpdate += onUpdate;

            registerHandlers();
        }

        public bool ownsBody(SyncBody body) {
            return body.owner == node.uid;
        }

        private void registerHandlers() {
            // presence
            handlers.register(new PresenceMessageHandler(this));
            handlers.register(new HeartbeatHandler(this));
            // body updates
            handlers.register(new BodyKinematicUpdateHandler(this));
            handlers.register(new BodyLifetimeUpdateHandler(this));
        }

        public virtual void stop() {
            node.stop(); // throw away our network node
        }

        public TGameUpdate createGameUpdate<TGameUpdate>() where TGameUpdate : GameUpdateMessage {
            var msg = node.getMessage<TGameUpdate>();
            msg.reset();
            return msg;
        }

        /// <summary>
        /// queue a game update to be sent at next update
        /// </summary>
        /// <param name="msg"></param>
        public void queueGameUpdate(GameUpdateMessage msg) {
            outgoingGameUpdates.Enqueue(msg);
        }

        /// <summary>
        /// immediately send a game update
        /// </summary>
        /// <param name="msg"></param>
        public void sendGameUpdate(GameUpdateMessage msg) {
            msg.sourceUid = node.uid;
            node.sendToAll(msg);
        }

        private void preprocessGameUpdate(GameUpdateMessage msg) {
            // check if we don't know the sender of this message, and then update their connectivity if necessary
            if (peers.All(x => x.uid != msg.sourceUid)) {
                // this is a relayed message, so we don't know the remId. we set an empty for now
                // var peer = new NetPlayer(msg.sourceUid);
                // peers.Add(peer);
                // Global.log.trace($"implicitly introduced to peer {peer} via {msg.GetType().Name}");
                // connectivityUpdates.Enqueue(new ConnectivityUpdate(peer,
                //     ConnectivityUpdate.ConnectionStatus.Connected));
            }
        }

        protected virtual void onUpdate() {
            // pump outgoing messages
            while (outgoingGameUpdates.TryDequeue(out var msg)) {
                sendGameUpdate(msg);
            }
        }

        protected virtual void onMessage(LimeMessage msg) {
            var msgType = msg.GetType();
            if (msg is GameUpdateMessage gameUpdateMessage) {
                // preprocess all game updates
                preprocessGameUpdate(gameUpdateMessage);
#if DEBUG
                if (debug) {
                    Global.log.trace($"received game update {gameUpdateMessage} from {msg.source}");
                }
#endif
            }
            else {
                // log misc message
#if DEBUG
                if (debug) {
                    Global.log.trace($"received message {msgType.Name} from {msg.source}");
                }
#endif
            }

            if (handlers.canHandle(msgType)) {
                var handler = handlers.resolve(msgType);
                var handled = handler.handle(msg);
            }
            else {
                Global.log.err($"no handler found for {msgType.Name}");
            }
        }

        protected virtual void onPeerConnected(NetConnection peer) {
            Global.log.info($"connected new peer {peer}");
            // once peer (server) connected, send intro
            // introduce myself to everyone else
            var intro = node.getMessage<PresenceMessage>();
            intro.myUid = node.uid;
            intro.here = true;
            intro.myNick = nickname;
            node.sendToAll(intro);
        }

        protected virtual void onPeerDisconnected(NetConnection peer) {
            Global.log.info($"disconnected peer {peer}");

            Global.log.err("confirmed disconnected from server");
            connected = false;
            connectionStatusChanged?.Invoke(connected);
        }

        public virtual void Dispose() {
            node.onPeerConnected = null;
            node.onPeerDisconnected = null;
            node.onMessage = null;
            node.onUpdate = null;
        }
    }
}