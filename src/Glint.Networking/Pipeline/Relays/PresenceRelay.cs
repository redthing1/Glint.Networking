using System.Collections.Generic;
using System.Linq;
using Glint.Networking.Game;
using Glint.Networking.Messages;

namespace Glint.Networking.Pipeline.Relays {
    public class PresenceRelay : ServerMessageRelay<PresenceMessage> {
        public PresenceRelay(GlintNetServerContext context) : base(context) { }

        protected override bool validate(PresenceMessage msg) {
            if (msg.here) {
                // ensure that the introduction is from a unique user
                return context.clients.All(x => x.uid != msg.myUid && x.uid != msg.myUid);
            } else {
                // ensure that the client already exists in our list
                return context.clients.Any(x => x.uid == msg.myUid);
            }
        }

        protected override ProcessResult process(PresenceMessage msg) {
            base.process(msg);
            var presence = msg.here ? "HERE" : "GONE";
            Global.log.info($"presence update from {msg.myUid}, {presence}");
            if (msg.here) {
                // save the user
                var player = new NetPlayer(msg.myUid, msg.myNick);
                context.server!.addPlayer(player);
                
                return ProcessResult.Relay;
            }
            
            // leave message broadcast is handled by "leftPlayerFollowUp"
            return ProcessResult.Done;
        }

        protected override void postprocess(PresenceMessage msg) {
            base.postprocess(msg);
            
            if (msg.here) {
                // since this user just joined, we want to introduce everyone else to them
                Global.log.trace($"resending {context.clients.Count} introductions");
                var player = context.clients.Single(x => x.uid == msg.myUid);
                foreach (var client in context.clients) {
                    var intro = context.serverNode.getMessage<PresenceMessage>();
                    intro.createFrom(client);
                    intro.here = true;
                    context.serverNode.sendTo(player.uid, intro);
                }
                
                context.server.joinedPlayerFollowUp(player);
            }
        }
    }
}