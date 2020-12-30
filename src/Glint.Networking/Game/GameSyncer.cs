using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Glint.Networking.Components;
using Glint.Networking.Game.Updates;
using Glint.Networking.Messages;
using Glint.Networking.Pipeline;
using Glint.Networking.Pipeline.Handlers;
using Glint.Networking.Pipeline.Messages;
using Glint.Networking.Utils.Collections;
using Glint.Util;
using Lidgren.Network;
using Lime;
using Lime.Messages;
using Nez;
using Random = Nez.Random;

namespace Glint.Networking.Game {
    /// <summary>
    /// connects to a game server and fill message queues with data
    /// </summary>
    public class GameSyncer : IDisposable {
        public LimeNode netNode { get; }
        public int netUps { get; }
        public int systemUps { get; }
        public int ringBufferSize { get; }
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
        public ConcurrentRingQueue<BodyUpdate> incomingBodyUpdates { get; }
        public ConcurrentQueue<GameUpdateMessage> outgoingGameUpdates { get; }
        
        protected ITimer nodeUpdateTimer;
        
        // - properties
        public long uid => netNode.uid;
        public string host { get; }
        public int port { get; }
        public string nickname { get; }
#if DEBUG
        public bool debug { get; }
#endif

        public GameSyncer(LimeNode node, string host, int port, string nickname, int netUps, int systemUps, int ringBufferSize,
            bool debug = false) {
            this.netNode = node;
            this.host = host;
            this.port = port;
            this.nickname = nickname;
            this.netUps = netUps;
            this.systemUps = systemUps;
            this.ringBufferSize = ringBufferSize;
#if DEBUG
            this.debug = debug;
#endif

            netNode.configureGlint();
            netNode.initialize();

            incomingBodyUpdates = new ConcurrentRingQueue<BodyUpdate>(this.ringBufferSize);
            outgoingGameUpdates = new ConcurrentQueue<GameUpdateMessage>();
            
            // wire events
            netNode.onPeerConnected += onPeerConnected;
            netNode.onPeerDisconnected += onPeerDisconnected;
            netNode.onMessage += onMessage;
            netNode.onUpdate += onUpdate;
            
            registerHandlers();
        }

        public bool ownsBody(SyncBody body) {
            return body.owner == netNode.uid;
        }

        private void registerHandlers() {
            // presence
            handlers.register(new PresenceMessageHandler(this));
            handlers.register(new HeartbeatHandler(this));
            // body updates
            handlers.register(new BodyKinematicUpdateHandler(this));
            handlers.register(new BodyLifetimeUpdateHandler(this));
        }

        public void stop() {
            netNode.stop(); // throw away our network node
        }

        public TGameUpdate createGameUpdate<TGameUpdate>() where TGameUpdate : GameUpdateMessage {
            var msg = netNode.getMessage<TGameUpdate>();
            msg.reset();
            return msg;
        }

        public void queueGameUpdate(GameUpdateMessage msg) {
            outgoingGameUpdates.Enqueue(msg);
        }

        public void sendGameUpdate(GameUpdateMessage msg) {
            msg.sourceUid = netNode.uid;
            netNode.sendToAll(msg);
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
        
        private void onUpdate() {
            // pump outgoing messages
            while (outgoingGameUpdates.TryDequeue(out var msg)) {
                sendGameUpdate(msg);
            }
        }

        public void onMessage(LimeMessage msg) {
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

        public void onPeerConnected(NetConnection peer) {
            Global.log.info($"connected new peer {peer}");
            // once peer (server) connected, send intro
            // introduce myself to everyone else
            var intro = netNode.getMessage<PresenceMessage>();
            intro.myUid = netNode.uid;
            intro.here = true;
            intro.myNick = nickname;
            netNode.sendToAll(intro);
        }

        public void onPeerDisconnected(NetConnection peer) {
            Global.log.info($"disconnected peer {peer}");

            Global.log.err("confirmed disconnected from server");
            connected = false;
            connectionStatusChanged?.Invoke(connected);
        }

        public void Dispose() {
            netNode.onPeerConnected = null;
            netNode.onPeerDisconnected = null;
            netNode.onMessage = null;
            netNode.onUpdate = null;
        }
    }
}