using System.Collections.Generic;
using Glint.Networking.Messages.Types;

namespace Glint.Networking.Game {
    public class NetScene {
        public struct Body {
            public long ownerUid;
            public long lastUpdateTime;
            public uint id;
            public uint syncTag;
            public PackedVec2 pos;
            public PackedVec2 vel;
            public float angle;
            public float angularVelocity;

            public Body(long ownerUid, long lastUpdateTime, uint id, uint syncTag, PackedVec2 pos, PackedVec2 vel,
                float angle, float angularVelocity) {
                this.ownerUid = ownerUid;
                this.lastUpdateTime = lastUpdateTime;
                this.id = id;
                this.syncTag = syncTag;
                this.pos = pos;
                this.vel = vel;
                this.angle = angle;
                this.angularVelocity = angularVelocity;
            }
        }

        public Dictionary<NetPlayer, List<Body>> bodies = new Dictionary<NetPlayer, List<Body>>();
    }
}