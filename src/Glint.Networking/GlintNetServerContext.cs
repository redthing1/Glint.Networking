using System.Collections.Generic;
using System.Reflection;
using Glint.Networking.Game;
using Glint.Util;
using Lime;

namespace Glint.Networking {
    /// <summary>
    /// context for server
    /// </summary>
    public class GlintNetServerContext {
        public Config config;
        public LimeServer serverNode;
        public List<GamePeer> clients = new List<GamePeer>();
        public List<Assembly> assemblies = new List<Assembly>();

        public class Config {
            public int port;
            public int updateInterval;
            public int heartbeatInterval = 2000;
            public Logger.Verbosity verbosity;
            public bool logMessages;
            public float timeout;
        }

        public GlintNetServerContext(Config config) {
            this.config = config;
        }
    }
}