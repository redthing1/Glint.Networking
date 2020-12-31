using MessagePack;
using Microsoft.Xna.Framework;

namespace Glint.Networking.Messages.Types {
    public class PackedVec2 {
        [Key(0)] public float x { get; set; }
        [Key(1)] public float y { get; set; }

        public PackedVec2() { }

        public PackedVec2(Vector2 v) : this(v.X, v.Y) { }

        public PackedVec2(float x, float y) {
            this.x = x;
            this.y = y;
        }

        public PackedVec2 copy() => new PackedVec2(x, y);

        public Vector2 unpack() => new Vector2(x, y);
    }
}