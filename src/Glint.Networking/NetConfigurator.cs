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
                        Global.log.writeLine(s, Logger.Verbosity.Trace);
                        break;
                    case LogLevel.Warning:
                        Global.log.writeLine(s, Logger.Verbosity.Warning);
                        break;
                    case LogLevel.Error:
                        Global.log.writeLine(s, Logger.Verbosity.Error);
                        break;
                }
            };
        }

        public static Assembly[] messageAssemblies => new[] {typeof(NetConfigurator).Assembly};
    }
}