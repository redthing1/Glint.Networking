using System.Linq;
using Glint.Networking.Game;
using Glint.Networking.Messages;
using Glint.Util;

namespace Glint.Networking.Handlers.Server {
    public class PresenceRelayHandler : ServerMessageRelay<PresenceMessage> {
        public PresenceRelayHandler(GlintNetServerContext context) : base(context) { }

        protected override bool validate(PresenceMessage msg) {
            if (msg.here) {
                // ensure that the introduction is unique
                return context.clients.All(x => x.uid != msg.myUid && x.remId != msg.myRemId);
            } else {
                // ensure that a client exists
                return context.clients.Any(x => x.uid == msg.myUid);
            }
        }

        protected override bool process(PresenceMessage msg) {
            base.process(msg);
            var presence = msg.here ? "HERE" : "GONE";
            Global.log.writeLine($"presence update from {msg.myRemId}, {presence}", GlintLogger.LogLevel.Information);
            if (msg.here) {
                // save the user
                var clientPeer = new GamePeer(msg.myRemId, msg.myUid);
                context.clients.Add(clientPeer);
                Global.log.writeLine($"added client {clientPeer}", GlintLogger.LogLevel.Trace);
                return true;
            }

            // we don't relay byes
            return false;
        }

        protected override void postprocess(PresenceMessage msg) {
            base.postprocess(msg);


            if (msg.here) {
                // whenever someone's new, we want to re-introduce everyone else to them
                Global.log.writeLine($"resending {context.clients.Count} introductions", GlintLogger.LogLevel.Trace);
                foreach (var client in context.clients) {
                    var intro = context.serverNode.getMessage<PresenceMessage>();
                    intro.createFrom(client);
                    intro.here = true;
                    context.serverNode.sendToAll(intro);
                }
            }
        }
    }
}