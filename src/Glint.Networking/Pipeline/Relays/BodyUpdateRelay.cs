using System.Linq;
using Glint.Networking.Game;
using Glint.Networking.Messages;
using Glint.Networking.Messages.Types;
using Glint.Networking.Utils;

namespace Glint.Networking.Pipeline.Relays {
    public abstract class BodyUpdateRelay<TMessage> : ServerMessageRelay<TMessage> where TMessage : BodyUpdateMessage {
        public BodyUpdateRelay(GlintNetServerContext context) : base(context) { }

        protected override bool validate(TMessage msg) {
            // validation logic for body updates?
            return true;
        }
    }

    public class BodyKinematicUpdateRelay : BodyUpdateRelay<BodyKinematicUpdateMessage> {
        public BodyKinematicUpdateRelay(GlintNetServerContext context) : base(context) { }
    }

    public class BodyLifetimeUpdateRelay : BodyUpdateRelay<BodyLifetimeUpdateMessage> {
        public BodyLifetimeUpdateRelay(GlintNetServerContext context) : base(context) { }

        protected override bool process(BodyLifetimeUpdateMessage msg) {
            var player = context.clients.SingleOrDefault(x => x.uid == msg.sourceUid);
            if (player == null)
                return false;
            var now = NetworkTime.time();
            if (msg.exists) {
                // create
                // TODO: assert not exists
                var userBodies = context.scene.bodies[player];
                if (userBodies.Any(x => x.id == msg.bodyId))
                    return false;
                userBodies.Add(new NetScene.Body(msg.sourceUid, now, msg.bodyId, msg.syncTag, new PackedVec2(0, 0),
                    new PackedVec2(0, 0), 0, 0));
            }
            else {
                // remove
                // TODO: assert exists
                var userBodies = context.scene.bodies[player];
                if (userBodies.All(x => x.id != msg.bodyId))
                    return false;
                userBodies.RemoveAll(x => x.id == msg.bodyId);
            }

            return true;
        }
    }
}