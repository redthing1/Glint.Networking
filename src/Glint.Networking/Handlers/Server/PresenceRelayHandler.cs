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
                return context.clients.All(x => x.uid != msg.myUid && x.nick != msg.myNick);
            } else {
                // ensure that a client exists
                return context.clients.Any(x => x.uid == msg.myUid);
            }
        }

        protected override bool process(PresenceMessage msg) {
            base.process(msg);
            var presence = msg.here ? "HERE" : "GONE";
            Global.log.info($"presence update from {msg.myNick}, {presence}");
            if (msg.here) {
                // save the user
                var clientPeer = new NetPlayer(msg.myNick, msg.myUid);
                context.clients.Add(clientPeer);
                Global.log.trace($"added client {clientPeer}");
                return true;
            }

            // we don't relay byes
            return false;
        }

        protected override void postprocess(PresenceMessage msg) {
            base.postprocess(msg);


            if (msg.here) {
                // whenever someone's new, we want to re-introduce everyone else to them
                Global.log.trace($"resending {context.clients.Count} introductions");
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