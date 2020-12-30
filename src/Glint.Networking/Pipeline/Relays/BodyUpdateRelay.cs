using Glint.Networking.Messages;

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
    }
}