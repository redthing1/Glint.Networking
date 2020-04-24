using Glint.Util;
using Lime.Messages;

namespace Glint.Networking.Handlers {
    public abstract class ServerMessageRelay<TMessage> : MessageHandler<TMessage> where TMessage : LimeMessage {
        protected GlintNetServerContext context;

        public ServerMessageRelay(GlintNetServerContext context) {
            this.context = context;
        }

        /// <summary>
        /// process the message before sending it. 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>whether we should relay the message</returns>
        protected virtual bool process(TMessage msg) {
            return true;
        }

        protected virtual void postprocess(TMessage msg) { }

        protected virtual bool validate(TMessage msg) => true;

        public override bool handle(TMessage msg) {
            var valid = validate(msg);
            if (valid && process(msg)) {
                context.serverNode.sendToAll(msg); // redistribute the message
                postprocess(msg);
                return true;
            }

            // log validation error
            Global.log.err($"validation failed for {msg}");

            return false;
        }
    }
}