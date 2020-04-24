using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Glint.Networking.Components;
using Glint.Networking.Game.Updates;
using Glint.Networking.Handlers;
using Glint.Networking.Handlers.Client;
using Glint.Networking.Messages;
using Glint.Networking.Utils.Collections;
using Glint.Util;
using Lidgren.Network;
using Lime;
using Lime.Messages;
using Nez;
using Random = Nez.Random;

namespace Glint.Networking.Game {
    public class GameSyncer {
        public LimeClient netNode { get; }
        public int netUps { get; }
        public int systemUps { get; }
        public int ringBufferSize { get; }
        public List<GamePeer> peers { get; } = new List<GamePeer>();
        public uint uid { get; } = (uint) Random.NextInt(int.MaxValue);
        public bool connected;
        public Action<bool> connectionStatusChanged;
        public MessageHandlerContainer handlerContainer { get; } = new MessageHandlerContainer();
        public Action<GamePeer> gamePeerConnected;
        public Action<GamePeer> gamePeerDisconnected;

        // message queues
        public ConcurrentQueue<ConnectivityUpdate> connectivityUpdates { get; } =
            new ConcurrentQueue<ConnectivityUpdate>();

        public ConcurrentRingQueue<BodyUpdate> bodyUpdates { get; }
        private ITimer nodeUpdateTimer;
        public string host { get; }
        public int port { get; }
#if DEBUG
        public bool debug { get; }
#endif

        public GameSyncer(string host, int port, int netUps, int systemUps, int ringBufferSize, float timeout,
            bool debug = false) {
            this.host = host;
            this.port = port;
            this.netUps = netUps;
            this.systemUps = systemUps;
            this.ringBufferSize = ringBufferSize;
#if DEBUG
            this.debug = debug;
#endif

            netNode = new LimeClient(new LimeNode.Configuration {
                peerConfig = new NetPeerConfiguration("Glint") {
                    ConnectionTimeout = timeout,
                    PingInterval = timeout / 2,
                },
                messageAssemblies = new[] {Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly()}
            });
            netNode.configureGlint();
            netNode.initialize();

            bodyUpdates = new ConcurrentRingQueue<BodyUpdate>(this.ringBufferSize);
            wireEvents();
            registerHandlers();
        }

        public bool ownsBody(SyncBody body) {
            return body.ownerUid == uid;
        }

        private void registerHandlers() {
            // presence
            handlerContainer.register(new PresenceMessageHandler(this));
            handlerContainer.register(new HeartbeatHandler(this));
            // body updates
            handlerContainer.registerAs<BodyUpdateMessage, BodyKinematicUpdateMessage>(
                new BodyUpdateHandler(this));
            handlerContainer.registerAs<BodyUpdateMessage, BodyLifetimeUpdateMessage>(
                new BodyUpdateHandler(this));
        }

        public void connect() {
            // open connection and go
            Global.log.writeLine($"connecting to server node ({host}:{port})",
                Logger.Verbosity.Information);
            netNode.start();
            netNode.connect(host, port, "hail");
            nodeUpdateTimer = Core.Schedule(1f / netUps, true, timer => { netNode.update(); });
        }

        public void disconnect() {
            Global.log.writeLine($"requesting disconnect from server",
                Logger.Verbosity.Information);
            var intro = netNode.getMessage<PresenceMessage>();
            intro.myRemId = netNode.remId;
            intro.myUid = uid;
            intro.here = false;
            netNode.sendToAll(intro);
            netNode.disconnect();
        }

        public void stop() {
            netNode.stop(); // throw away our network node
        }

        public void wireEvents() {
            netNode.onPeerConnected += onPeerConnected;
            netNode.onPeerDisconnected += onPeerDisconnected;
            netNode.onMessage += onMessage;
        }

        public TGameUpdate createGameUpdate<TGameUpdate>() where TGameUpdate : GameUpdateMessage {
            var msg = netNode.getMessage<TGameUpdate>();
            msg.reset();
            return msg;
        }

        public void sendGameUpdate(GameUpdateMessage msg) {
            msg.sourceUid = uid;
            netNode.sendToAll(msg);
        }

        private void preprocessGameUpdate(GameUpdateMessage msg) {
            // check if we don't know the sender of this message, and then update their connectivity if necessary
            if (peers.All(x => x.uid != msg.sourceUid)) {
                // this is a relayed message, so we don't know the remId. we set an empty for now
                var peer = new GamePeer(0, msg.sourceUid);
                peers.Add(peer);
                Global.log.writeLine($"implicitly introduced to peer {peer}", Logger.Verbosity.Trace);
                connectivityUpdates.Enqueue(new ConnectivityUpdate(peer,
                    ConnectivityUpdate.ConnectionStatus.Connected));
            }
        }

        public void onMessage(LimeMessage msg) {
            var msgType = msg.GetType();
            if (msg is GameUpdateMessage gameUpdateMessage) { // preprocess all game updates
                preprocessGameUpdate(gameUpdateMessage);
#if DEBUG
                if (debug) {
                    Global.log.writeLine($"received game update {gameUpdateMessage} from {msg.source}",
                        Logger.Verbosity.Trace);
                }
#endif
            } else { // log misc message
#if DEBUG
                if (debug) {
                    Global.log.writeLine($"received message {msgType.Name} from {msg.source}",
                        Logger.Verbosity.Trace);
                }
#endif
            }

            if (handlerContainer.canHandle(msgType)) {
                var handler = handlerContainer.resolve(msgType);
                var handled = handler.handle(msg);
            } else {
                Global.log.writeLine($"no handler found for {msgType.Name}", Logger.Verbosity.Error);
            }
        }

        public void onPeerConnected(NetConnection peer) {
            Global.log.writeLine($"connected new peer {peer}", Logger.Verbosity.Information);
            // once peer (server) connected, send intro
            var intro = netNode.getMessage<PresenceMessage>();
            intro.myRemId = netNode.remId;
            intro.myUid = uid;
            intro.here = true;
            netNode.sendToAll(intro);
        }

        public void onPeerDisconnected(NetConnection peer) {
            Global.log.writeLine($"disconnected peer {peer}", Logger.Verbosity.Information);

            Global.log.writeLine("confirmed disconnected from server", Logger.Verbosity.Error);
            connected = false;
            connectionStatusChanged?.Invoke(connected);
        }
    }
}
