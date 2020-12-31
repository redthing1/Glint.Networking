using Lime.Messages;

namespace Glint.Networking.Pipeline {
    public interface IMessageHandler {
        /// <summary>
        /// whether the message was handled successfully
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        bool handle(LimeMessage msg);
    }
}