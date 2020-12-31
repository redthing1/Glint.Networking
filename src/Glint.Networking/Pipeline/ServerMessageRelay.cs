using System;
using Lime.Messages;
using Nez;

namespace Glint.Networking.Pipeline {
    public abstract class ServerMessageRelay<TMessage> : MessageHandler<TMessage> where TMessage : LimeMessage {
        protected GlintNetServerContext context;

        public ServerMessageRelay(GlintNetServerContext context) {
            this.context = context;
        }

        [Flags]
        public enum ProcessResult : int {
            /// <summary>
            /// process failed
            /// </summary>
            Fail = 1 << 0,

            /// <summary>
            /// process succeeded
            /// </summary>
            OK = 1 << 1,

            /// <summary>
            /// message should be echoed to all other clients
            /// </summary>
            Echo = 1 << 2,

            /// <summary>
            /// process succeeded, processing complete (don't send to other clients)
            /// </summary>
            Done = OK,

            /// <summary>
            /// process succeeded, echo message to all other clients
            /// </summary>
            Relay = OK | Echo,
        }

        /// <summary>
        /// process the message before sending it. 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>whether we should relay the message</returns>
        protected virtual ProcessResult process(TMessage msg) {
            return ProcessResult.Relay;
        }

        protected virtual void postprocess(TMessage msg) { }

        protected virtual bool validate(TMessage msg) => true;

        public override bool handle(TMessage msg) {
            var uid = msg.source.RemoteUniqueIdentifier;
            if (validate(msg)) {
                var result = process(msg);
                if (result.HasFlag(ProcessResult.OK)) {
                    if (result.HasFlag(ProcessResult.Echo)) {
                        context.serverNode!.sendToAll(msg); // redistribute the message
                    }

                    postprocess(msg);
                    return true;
                }
                else if (result.HasFlag(ProcessResult.Fail)) {
                    Global.log.err($"process failed for {msg} (from {uid})");
                }
            }
            else {
                Global.log.err($"pre-validation failed for {msg} (from {uid})");
            }

            return false;
        }
    }
}