using System;
using System.Collections.Generic;
using Lime.Messages;

namespace Glint.Networking.Handlers {
    public class MessageHandlerContainer {
        private Dictionary<Type, IMessageHandler> handlers = new Dictionary<Type, IMessageHandler>();

        public MessageHandler<TMessage> getHandler<TMessage>() where TMessage : LimeMessage =>
            (MessageHandler<TMessage>) handlers[typeof(TMessage)];

        public void register<TMessage>(MessageHandler<TMessage> handler) where TMessage : LimeMessage =>
            registerAs<TMessage, TMessage>(handler);

        public void registerAs<TRegistration, TMessage>(MessageHandler<TRegistration> handler)
            where TMessage : LimeMessage where TRegistration : LimeMessage {
            handlers[typeof(TMessage)] = handler;
        }

        public bool canHandle(Type type) => handlers.ContainsKey(type);

        public bool canHandle<TMessage>() where TMessage : LimeMessage => canHandle(typeof(TMessage));

        public IMessageHandler resolve(Type type) => handlers[type];

        public MessageHandler<TMessage> resolve<TMessage>() where TMessage : LimeMessage =>
            (MessageHandler<TMessage>) resolve(typeof(TMessage));
    }
}