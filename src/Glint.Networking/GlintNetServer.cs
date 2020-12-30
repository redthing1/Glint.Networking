using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Glint.Networking.Game;
using Glint.Networking.Messages;
using Glint.Networking.Pipeline;
using Glint.Networking.Pipeline.Relays;
using Lidgren.Network;
using Lime;
using Lime.Messages;

namespace Glint.Networking {
    /// <summary>
    /// the main game synchronization server
    /// </summary>
    public class GlintNetServer {
        /// <summary>
        /// default port
        /// </summary>
        public const int DEF_PORT = 13887;

        /// <summary>
        /// default timeout in seconds
        /// </summary>
        public const int DEF_TIMEOUT = 10;

        /// <summary>
        /// default update rate (per second)
        /// </summary>
        public const int DEF_UPS = 10;

        /// <summary>
        /// default app id
        /// </summary>
        public const string DEF_APP_ID = "Glint";

        public GlintNetServerContext context;

        /// <summary>
        /// contains the handlers for each message type
        /// </summary>
        public MessageHandlerContainer handlers = new MessageHandlerContainer();

        public LimeServer node;

        private Stopwatch stopwatch;

        // handlers to be hooked by a scene
        public Action<NetPlayer>? onClientJoin;
        public Action<NetPlayer>? onClientLeave;

        public GlintNetServer(GlintNetServerContext.Config config) {
            context = new GlintNetServerContext(config);
            context.server = this;
        }

        private void configureDefaultHandlers() {
            // presence
            handlers.register(new PresenceRelay(context));
            // body updates
            handlers.register(new BodyKinematicUpdateRelay(context));
            handlers.register(new BodyLifetimeUpdateRelay(context));
        }

        public void initialize() {
            configureDefaultHandlers();
        }

        public void run(CancellationTokenSource? tokenSource = null) {
            var peerConfig =
                NetConfigurator.createServerPeerConfig(context.config.protocol, context.config.port,
                    context.config.timeout);
#if DEBUG
            if (context.config.simulateLag) {
                Global.log.warn("lag simulation enabled");
                peerConfig.SimulatedLoss = 0.1f;
                peerConfig.SimulatedRandomLatency = 0.5f;
            }
#endif
            node = new LimeServer(new LimeNode.Configuration {
                peerConfig = peerConfig,
                messageAssemblies = new[] {Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly()}
                    .Concat(context.assemblies).ToArray()
            });
            // connect node hooks to glint (logs)
            node.configureGlint();
            node.initialize();

            Global.log.info("initialized networking host");

            // log config in trace
            Global.log.trace($"timeout: {context.config.timeout:n2}s");
            Global.log.trace($"update: {1000 / context.config.ups}ms");

            context.serverNode = node;

            // wire callbacks
            node.onPeerConnected += onPeerConnected;
            node.onPeerDisconnected += onPeerDisconnected;
            node.onMessage += onMessage;

            // start server
            node.start();
            Global.log.info($"created server node on port {node.lidgrenServer.Port}");

            stopwatch = new Stopwatch();
            stopwatch.Start();
            var nextHeartbeat = 0L;

            while (!tokenSource?.IsCancellationRequested ?? true) {
                if (stopwatch.ElapsedMilliseconds > nextHeartbeat) {
                    // send heartbeat
                    var beat = node.getMessage<HeartbeatMessage>();
                    beat.alive = true;
                    node.sendToAll(beat);
                    nextHeartbeat = stopwatch.ElapsedMilliseconds + context.config.heartbeatInterval;
                }

                node.update();
                Thread.Sleep(1000 / context.config.ups);
            }

            stopwatch.Stop();
        }

        private void onMessage(LimeMessage msg) {
            var msgType = msg.GetType();
            if (context.config.logMessages) {
                Global.log.trace($"received message {msgType.Name} from {msg.source}");
            }

            // dynamically resolve the handlers
            if (handlers.canHandle(msgType)) {
                var handler = handlers.resolve(msgType);
                var status = handler.handle(msg);
            }
            else {
                Global.log.err($"no handler found for {msgType.Name}");
            }

            // update last message time
            var client = context.clients.Single(x => x.uid == msg.source.RemoteUniqueIdentifier);
            client.lastMessage = stopwatch.ElapsedMilliseconds;
        }

        private void onPeerConnected(NetConnection peer) {
            Global.log.info($"connected new peer {peer} (before: {context.clients.Count})");
            // note that the peer won't be added until it sends a PresenceMessage
        }

        private void onPeerDisconnected(NetConnection peer) {
            Global.log.info($"disconnected peer {peer} (before: {context.clients.Count})");
            // broadcast a goodbye on behalf of that peer
            var player = context.clients.SingleOrDefault(x => x.uid == peer.RemoteUniqueIdentifier);
            if (player == null) {
                Global.log.err($"failed to send goodbyes on behalf of nonexistent peer {peer.RemoteUniqueIdentifier}");
                return;
            }
            
            removePlayer(player);
        }

        internal void addPlayer(NetPlayer player) {
            context.clients.Add(player);
            context.scene.bodies[player] = new List<NetScene.Body>();
            onClientJoin?.Invoke(player); // call handler
            Global.log.trace($"added client {player}");
            onJoinHook(player);
        }

        internal void removePlayer(NetPlayer player) {
            context.server!.onClientLeave?.Invoke(player);
            context.scene.bodies.Remove(player);
            context.clients.Remove(player);
            Global.log.trace($"removed client {player}");
            onLeaveHook(player);
        }

        private void onJoinHook(NetPlayer netPlayer) {
            // send catch up
        }

        private void onLeaveHook(NetPlayer netPlayer) {
            
        }
    }
}