using Lime.Messages;

namespace Glint.Networking.Pipeline {
    public interface IMessageHandler {
        bool handle(LimeMessage msg);
    }
}