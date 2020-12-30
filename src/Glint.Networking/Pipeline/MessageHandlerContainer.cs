using System;
using System.Collections.Generic;
using Lime.Messages;

namespace Glint.Networking.Pipeline {
    public class MessageHandlerContainer {
        private Dictionary<Type, IMessageHandler> handlers = new Dictionary<Type, IMessageHandler>();

        public MessageHandler<TMessage> getHandler<TMessage>() where TMessage : LimeMessage =>
            (MessageHandler<TMessage>) handlers[typeof(TMessage)];

        /// <summary>
        /// register a handler for a single type of message
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="TMessage"></typeparam>
        public void register<TMessage>(MessageHandler<TMessage> handler) where TMessage : LimeMessage {
            handlers[typeof(TMessage)] = handler;
        }

        public void unregister<TMessage>() where TMessage : LimeMessage {
            handlers.Remove(typeof(TMessage));
        }

        public bool canHandle(Type type) => handlers.ContainsKey(type);

        public bool canHandle<TMessage>() where TMessage : LimeMessage => canHandle(typeof(TMessage));

        public IMessageHandler resolve(Type type) => handlers[type];

        public MessageHandler<TMessage> resolve<TMessage>() where TMessage : LimeMessage =>
            (MessageHandler<TMessage>) resolve(typeof(TMessage));
    }
}