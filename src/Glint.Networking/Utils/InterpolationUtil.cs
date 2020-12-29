using Microsoft.Xna.Framework;

namespace Glint.Networking.Utils {
    public static class InterpolationUtil {
        public static Vector2 lerp(Vector2 start, Vector2 end, float t) {
            return start + (end - start) * t;
        }

        public static float lerp(float start, float end, float t) {
            return start + (end - start) * t;
        }

        public static Vector2 hermite(Vector2 start, Vector2 end, Vector2 startVel, Vector2 endVel, float t) {
            var x = hermite(start.X, end.X, startVel.X, endVel.X, t);
            var y = hermite(start.Y, end.Y, startVel.Y, endVel.Y, t);
            return new Vector2(x, y);
        }

        public static float hermite(float start, float end, float startVel, float endVel, float t) {
            return MathHelper.Hermite(start, startVel, end, endVel, t);
        }
    }
}