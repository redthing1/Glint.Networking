using System;

namespace Glint.Networking.Utils {
    public static class NetworkTime {
        public static readonly long startTime = time();
        public static long time() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public static long timeSinceStart => time() - startTime;
    }
}