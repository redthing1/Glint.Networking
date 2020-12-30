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
        public List<Assembly> assemblies = new List<Assembly>();
        public LimeServer? serverNode;
        public GlintNetServer? server;
        public List<NetPlayer> clients = new List<NetPlayer>();
        public NetScene scene = new NetScene();

        public class Config {
            public string protocol = "Glint-Net";
            public int port;
            public int ups;
            public int heartbeatInterval = 2000;
            public bool logMessages;
            public float timeout;
#if DEBUG
            public bool simulateLag = false;
#endif
        }

        public GlintNetServerContext(Config config) {
            this.config = config;
        }
    }
}