using System.Reflection;
using Glint.Util;
using Lidgren.Network;
using Lime;
using Lime.Utils;

namespace Glint.Networking {
    public static class NetConfigurator {
        public static void configureGlint(this LimeNode node) {
            // register lime logging handler
            node.onLog += (level, s) => {
                switch (level) {
                    case Verbosity.Trace:
                        Global.log.trace(s);
                        break;
                    case Verbosity.Warning:
                        Global.log.warn(s);
                        break;
                    case Verbosity.Error:
                        Global.log.err(s);
                        break;
                }
            };
        }

        public static NetPeerConfiguration createServerPeerConfig(int port, float timeout) {
            return new NetPeerConfiguration(GlintNetServer.DEF_APP_ID) {
                Port = port,
                ConnectionTimeout = timeout,
                PingInterval = timeout / 2,
            };
        }

        public static NetPeerConfiguration createClientPeerConfig(float timeout) {
            return new NetPeerConfiguration(GlintNetServer.DEF_APP_ID) {
                ConnectionTimeout = timeout,
                PingInterval = timeout / 2,
            };
        }

        public static Assembly[] messageAssemblies => new[] {typeof(NetConfigurator).Assembly};
    }
}