using Glint.Networking.Messages;

namespace Glint.Networking.Handlers.Server {
    public class BodyRelayMessageHandler : ServerMessageRelay<BodyUpdateMessage> {
        public BodyRelayMessageHandler(GlintNetServerContext context) : base(context) { }

        protected override bool validate(BodyUpdateMessage msg) {
            // TODO: proper validation logic for body updates
            return true;
        }
    }
}