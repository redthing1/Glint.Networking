using System.Linq;
using Glint.Networking.Game;
using Glint.Networking.Messages;

namespace Glint.Networking.Pipeline.Relays {
    public class PresenceRelay : ServerMessageRelay<PresenceMessage> {
        public PresenceRelay(GlintNetServerContext context) : base(context) { }

        protected override bool validate(PresenceMessage msg) {
            if (msg.here) {
                // ensure that the introduction is from a unique user
                var unique = context.clients.All(x => x.uid != msg.myUid);
                if (!unique) {
                    Global.log.trace(
                        $"presence update (HERE) was for player who is already here: {msg} (from {msg.myUid})");
                    return false;
                }

                return true;
            }
            else {
                // ensure that the client already exists in our list
                var exists = context.clients.SingleOrDefault(x => x.uid == msg.myUid);
                if (exists == null) {
                    Global.log.trace($"presence update (GONE) was for unknown player: {msg} (from {msg.myUid})");
                    return false;
                }

                return true;
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