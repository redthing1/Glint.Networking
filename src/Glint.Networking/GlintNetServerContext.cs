using System.Collections.Generic;
using System.Reflection;
using Glint.Networking.Game;
using Glint.Util;
using Lime;

namespace Glint.Networking {
    /// <summary>
    /// context for server, that is passed to handlers
    /// </summary>
    public class GlintNetServerContext {
        public Config config;
        public LimeServer? serverNode;
        public GlintNetServer? server;
        public List<NetPlayer> clients = new List<NetPlayer>();
        public NetScene scene = new NetScene();

        public class Config {
            public int ups;
            public int heartbeatInterval = 2000;
            public bool logMessages;
        }

        public GlintNetServerContext(Config config) {
            this.config = config;
        }
    }
}