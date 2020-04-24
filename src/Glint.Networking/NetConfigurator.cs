using System.Reflection;
using Glint.Util;
using Lime;
using Lime.Utils;

namespace Glint.Networking {
    public static class NetConfigurator {
        public static void configureGlint(this LimeNode node) {
            node.onLog += (level, s) => {
                switch (level) {
                    case LogLevel.Trace:
                        Global.log.trace(s);
                        break;
                    case LogLevel.Warning:
                        Global.log.warn(s);
                        break;
                    case LogLevel.Error:
                        Global.log.err(s);
                        break;
                }
            };
        }

        public static Assembly[] messageAssemblies => new[] {typeof(NetConfigurator).Assembly};
    }
}