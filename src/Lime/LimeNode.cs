using System;
using System.Reflection;
using Lidgren.Network;
using Lime.Messages;
using Lime.Utils;

namespace Lime {
    public abstract class LimeNode {
        protected NetPeer lidgrenPeer;
        public long remId => lidgrenPeer.UniqueIdentifier;
        public Action<Verbosity, string> onLog = (level, s) => { /* default is to just discard */ };
        public Action<NetConnection> onPeerConnected;
        public Action<NetConnection> onPeerDisconnected;
        public Action<LimeMessage> onMessage;
        private LimeMessageFactory msgFactory = new LimeMessageFactory();
        public Configuration config { get; }

        public class Configuration {
            public NetPeerConfiguration peerConfig;
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
                                onPeerConnected(msg.SenderConnection);
                                break;
                            case NetConnectionStatus.Disconnected:
                                onPeerDisconnected(msg.SenderConnection);
                                break;
                        }

                        break;
                    case NetIncomingMessageType.Data:
                        // data packets are always serialized messages
                        var rawPacketData = msg.ReadBytes(msg.LengthBytes);
                        var message = msgFactory.read(rawPacketData);
                        message.source = msg.SenderConnection;
                        onMessage(message);
                        break;
                    default:
                        onLog(Verbosity.Warning, $"unhandled message type {msg.MessageType}");
                        break;
                }

                lidgrenPeer.Recycle(msg);
            }
        }

        public T getMessage<T>() where T : LimeMessage {
            return msgFactory.get<T>();
        }

        public void sendTo(NetConnection conn, LimeMessage message) {
            var packet = lidgrenPeer.CreateMessage();
            message.write(packet);
            lidgrenPeer.SendMessage(packet, conn, message.deliveryMethod);
        }

        public void sendToAll(LimeMessage message) {
            foreach (var conn in lidgrenPeer.Connections) {
                sendTo(conn, message);
            }
        }
    }
}