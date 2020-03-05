using System;
using System.Collections.Generic;
using System.Reflection;
using Lime.Messages;
using Lime.Utils;
using scopely.msgpacksharp;

namespace Lime {
    public class LimeMessageFactory {
        private Dictionary<byte, LimeMessage> messages = new Dictionary<byte, LimeMessage>();
        private Dictionary<Type, LimeMessage> messagesByType = new Dictionary<Type, LimeMessage>();
        private Action<LogLevel, string> log;

        internal LimeMessageFactory() { }

        public void init(Assembly[] messageAssemblies, Action<LogLevel, string> onLog) {
            log = onLog;
            buildInstances(messageAssemblies);
        }

        public T get<T>() where T : LimeMessage {
            LimeMessage result = null;
            if (!messagesByType.TryGetValue(typeof(T), out result))
                throw new ApplicationException("Attempt to get network message of type " + typeof(T) +
                                               " failed because that message type hasn't been registered");
            return (T) result;
        }

        public LimeMessage read(byte[] buffer) {
            int offset = 1;
            var result = messages[buffer[0]]; // get matching instance
            MsgPackSerializer.DeserializeObject(result, buffer, offset);
            return result;
        }

        private void buildInstances(Assembly[] messageAssemblies) {
            buildInstances(typeof(LimeMessage).Assembly);
            foreach (Assembly a in messageAssemblies)
                buildInstances(a);
            log(LogLevel.Trace, $"created instances for {messages.Count} registered network messages");
        }

        private void buildInstances(Assembly messageAssembly) {
            Type networkMsgType = typeof(LimeMessage);
            foreach (Type type in messageAssembly.GetTypes()) {
                if (type.IsSubclassOf(networkMsgType) && !messagesByType.ContainsKey(type)) {
                    if (messages.Count == Byte.MaxValue)
                        throw new ApplicationException(
                            "The maximum number of network messages has been reached - you need to use fewer message types in this project");
                    var msg = (LimeMessage) type.GetConstructor(Type.EmptyTypes)?.Invoke(new object[] { });
                    if (msg == null) {
                        throw new ApplicationException($"no valid constructor found for message {type}");
                    }

                    msg.id = (byte) messages.Count;
                    messages[msg.id] = msg;
                    messagesByType[msg.GetType()] = msg;
                    log(LogLevel.Trace, $"registered message type {msg.GetType()} as id {msg.id}");
                }
            }
        }
    }
}