using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Glint.Networking.Handlers;
using Glint.Networking.Handlers.Server;
using Glint.Networking.Messages;
using Glint.Util;
using Lidgren.Network;
using Lime;
using Lime.Messages;

namespace Glint.Networking {
    /// <summary>
    /// the main game synchronization server
    /// </summary>
    public class GlintNetServer {
        public const int DEF_PORT = 13887;
        public const int DEF_TIMEOUT = 10;
        public const int DEF_INTERVAL = 100;

        public GlintNetServerContext context;
        public MessageHandlerContainer handlerContainer = new MessageHandlerContainer();

        public GlintNetServer(GlintNetServerContext.Config config) {
            context = new GlintNetServerContext(config);
        }

        private void configureDefaultHandlers() {
            // presence
            handlerContainer.register(new PresenceRelayHandler(context));
            // body updates
            handlerContainer.registerAs<BodyUpdateMessage, BodyKinematicUpdateMessage>(
                new BodyRelayMessageHandler(context));
            handlerContainer.registerAs<BodyUpdateMessage, BodyLifetimeUpdateMessage>(
                new BodyRelayMessageHandler(context));
        }

        public void configure() {
            Global.log.verbosity = context.config.verbosity;
            configureDefaultHandlers();
        }

        public void run(CancellationTokenSource tokenSource = null) {
            var serverNode = new LimeServer(new LimeNode.Configuration {
                peerConfig = new NetPeerConfiguration("Glint") {
                    Port = context.config.port,
                    ConnectionTimeout = context.config.timeout,
                    PingInterval = context.config.timeout / 2,
                },
                messageAssemblies = new[] {Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly()}
                    .Concat(context.assemblies).ToArray()
            });
            serverNode.configureGlint();
            serverNode.initialize();

            Global.log.writeLine("configured networking host", GlintLogger.LogLevel.Information);

            // log config in trace
            Global.log.writeLine($"timeout: {context.config.timeout:n2}s", GlintLogger.LogLevel.Trace);
            Global.log.writeLine($"update: {context.config.updateInterval}ms", GlintLogger.LogLevel.Trace);

            context.serverNode = serverNode;
            serverNode.onPeerConnected += onPeerConnected;
            serverNode.onPeerDisconnected += onPeerDisconnected;
            serverNode.onMessage += onMessage;
            serverNode.start();
            Global.log.writeLine($"created server node on port {serverNode.lidgrenServer.Port}",
                GlintLogger.LogLevel.Information);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var nextHeartbeat = 0L;
            var lastTime = 0L;
            // TODO: run a console
            while (!tokenSource?.IsCancellationRequested ?? true) {
                if (stopwatch.ElapsedMilliseconds > nextHeartbeat) {
                    // send heartbeat
                    var beat = serverNode.getMessage<HeartbeatMessage>();
                    beat.alive = true;
                    serverNode.sendToAll(beat);
                    nextHeartbeat = stopwatch.ElapsedMilliseconds + context.config.heartbeatInterval;
                }

                serverNode.update();
                Thread.Sleep(context.config.updateInterval);
            }

            stopwatch.Stop();
        }

        private void onMessage(LimeMessage msg) {
            var msgType = msg.GetType();
            if (context.config.logMessages) {
                Global.log.writeLine($"received message {msgType.Name} from {msg.source}",
                    GlintLogger.LogLevel.Trace);
            }

            // dynamically resolve the handlers
            if (handlerContainer.canHandle(msgType)) {
                var handler = handlerContainer.resolve(msgType);
                handler.handle(msg);
            } else {
                Global.log.writeLine($"no handler found for {msgType.Name}", GlintLogger.LogLevel.Error);
            }
        }

        private void onPeerConnected(NetConnection peer) {
            Global.log.writeLine($"connected new peer {peer} (before: {context.clients.Count})",
                GlintLogger.LogLevel.Information);
        }

        private void onPeerDisconnected(NetConnection peer) {
            Global.log.writeLine($"disconnected peer {peer} (before: {context.clients.Count})",
                GlintLogger.LogLevel.Information);
            // broadcast a goodbye on behalf of that peer
            var clientPeer = context.clients.SingleOrDefault(x => x.remId == peer.RemoteUniqueIdentifier);
            if (clientPeer == null) {
                Global.log.writeLine($"failed to send goodbye for nonexistent peer {peer.RemoteUniqueIdentifier}",
                    GlintLogger.LogLevel.Error);
                return;
            }

            // remove the user
            context.clients.Remove(clientPeer);
            Global.log.writeLine($"removed client {clientPeer}", GlintLogger.LogLevel.Trace);

            Global.log.writeLine($"sending goodbye on behalf of {peer.RemoteUniqueIdentifier}",
                GlintLogger.LogLevel.Trace);
            var bye = context.serverNode.getMessage<PresenceMessage>();
            bye.createFrom(clientPeer);
            bye.here = false;
            context.serverNode.sendToAll(bye);
        }
    }
}