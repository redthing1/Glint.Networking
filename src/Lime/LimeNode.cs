using System;
using System.Reflection;
using Lidgren.Network;
using Lime.Messages;
using Lime.Utils;

namespace Lime {
    public abstract class LimeNode {
        protected NetPeer lidgrenPeer;
        public long uid => lidgrenPeer.UniqueIdentifier;

        public Action<Verbosity, string> onLog = (level, s) => { /* default is to just discard */
        };

        public Action<NetConnection>? onPeerConnected;
        public Action<NetConnection>? onPeerDisconnected;
        public Action<LimeMessage>? onMessage;
        public Action? onUpdate;
        private LimeMessageFactory msgFactory = new LimeMessageFactory();
        public Configuration config { get; }

        public class Configuration {
            public NetPeerConfiguration peerConfig;

            /// <summary>
            /// the assemblies from which to load message types. this must be identical (including order) on client and server!
            /// </summary>
            public Assembly[] messageAssemblies;
        }

        protected LimeNode(Configuration config, NetPeer peer) {
            this.config = config;
            lidgrenPeer = peer;
        }

        public virtual void initialize() {
            msgFactory.init(config.messageAssemblies, onLog);
        }

        public virtual void start() {
            lidgrenPeer.Start();
        }

        public virtual void stop() {
            // disconnect all clients
            foreach (var client in lidgrenPeer.Connections) {
                client.Disconnect("stop");
            }
        }

        /// <summary>
        /// pump the message queue
        /// </summary>
        public void update() {
            var msg = default(NetIncomingMessage);
            while ((msg = lidgrenPeer.ReadMessage()) != null) {
                switch (msg.MessageType) {
                    case NetIncomingMessageType.VerboseDebugMessage:
                        onLog(Verbosity.Trace, msg.ReadString());
                        break;
                    case NetIncomingMessageType.DebugMessage:
                        onLog(Verbosity.Trace, msg.ReadString());
                        break;
                    case NetIncomingMessageType.WarningMessage:
                        onLog(Verbosity.Warning, msg.ReadString());
                        break;
                    case NetIncomingMessageType.ErrorMessage:
                        onLog(Verbosity.Error, msg.ReadString());
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        NetConnectionStatus status = (NetConnectionStatus) msg.ReadByte();

                        var reason = msg.ReadString();
                        switch (status) {
                            case NetConnectionStatus.Connected:
                                onPeerConnected?.Invoke(msg.SenderConnection);
                                break;
                            case NetConnectionStatus.Disconnected:
                                onPeerDisconnected?.Invoke(msg.SenderConnection);
                                break;
                        }

                        break;
                    case NetIncomingMessageType.Data:
                        // data packets are always serialized messages
                        var rawPacketData = msg.ReadBytes(msg.LengthBytes);
                        var message = msgFactory.read(rawPacketData);
                        if (message == null) {
                            // could not deserialize
                            onLog(Verbosity.Error, $"failed to deserialize message of length {rawPacketData.Length}");
                            break;
                        }

                        message.source = msg.SenderConnection;
                        onMessage?.Invoke(message);
                        break;
                    default:
                        onLog(Verbosity.Warning, $"unhandled message type {msg.MessageType}");
                        break;
                }

                lidgrenPeer.Recycle(msg);
            }

            onUpdate?.Invoke();
        }

        public T getMessage<T>() where T : LimeMessage {
            return msgFactory.get<T>();
        }

        private NetConnection? getConnByUid(long peerUid) {
            for (var i = 0; i < lidgrenPeer.Connections.Count; i++) {
                var conn = lidgrenPeer.Connections[i];
                if (conn.RemoteUniqueIdentifier == peerUid) return conn;
            }

            return null;
        }

        public void sendTo(long peerUid, LimeMessage message) {
            var conn = getConnByUid(peerUid);
            if (conn == null) {
                throw new ApplicationException("tried to send message to nonexistent peer");
            }

            sendTo(conn, message);
        }

        public void sendTo(NetConnection conn, LimeMessage message) {
            var packet = lidgrenPeer.CreateMessage();
            msgFactory.write(packet, message);
            lidgrenPeer.SendMessage(packet, conn, message.deliveryMethod);
        }

        public void sendToAll(LimeMessage message) {
            foreach (var conn in lidgrenPeer.Connections) {
                sendTo(conn, message);
            }
        }

        public NetConnectionStatistics? connectionStatistics(long peerUid) {
            var conn = getConnByUid(peerUid);
            if (conn == null) {
                return null;
            }

            return conn.Statistics;
        }

        public NetPeerStatistics nodeStatistics() {
            return lidgrenPeer.Statistics;
        }
    }
}