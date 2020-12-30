using Glint.Networking.Messages;

namespace Glint.Networking.Handlers.Server {
    public abstract class BodyUpdateRelay : ServerMessageRelay<BodyUpdateMessage> {
        public BodyUpdateRelay(GlintNetServerContext context) : base(context) { }

        protected override bool validate(BodyUpdateMessage msg) {
            // TODO: proper validation logic for body updates
            return true;
        }
    }

    public class BodyKinematicUpdateRelay : BodyUpdateRelay {
        public BodyKinematicUpdateRelay(GlintNetServerContext context) : base(context) { }
    }

    public class BodyLifetimeUpdateRelay : BodyUpdateRelay {
        public BodyLifetimeUpdateRelay(GlintNetServerContext context) : base(context) { }
    }
}