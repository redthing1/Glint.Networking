using System;
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

        public static NetPeerConfiguration createServerPeerConfig(string protocol, int port, float timeout) {
            return new NetPeerConfiguration(protocol) {
                Port = port,
                ConnectionTimeout = TimeSpan.FromSeconds(timeout),
                PingInterval = TimeSpan.FromSeconds(timeout / 2),
            };
        }

        public static NetPeerConfiguration createClientPeerConfig(string protocol, float timeout) {
            return new NetPeerConfiguration(protocol) {
                ConnectionTimeout = TimeSpan.FromSeconds(timeout),
                PingInterval = TimeSpan.FromSeconds(timeout / 2),
            };
        }

        public static Assembly[] messageAssemblies => new[] {typeof(NetConfigurator).Assembly};
    }
}