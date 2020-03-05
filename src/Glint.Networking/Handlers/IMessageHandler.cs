using Lime.Messages;

namespace Glint.Networking.Handlers {
    public interface IMessageHandler {
        bool handle(LimeMessage msg);
    }
}