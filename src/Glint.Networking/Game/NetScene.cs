using System.Collections.Generic;
using Glint.Networking.Messages;
using Glint.Networking.Messages.Types;
using Glint.Networking.Pipeline.Messages;

namespace Glint.Networking.Game {
    public class NetScene {
        public class Body {
            public long ownerUid;
            public long lastSnapshotTime;
            public long lastReceivedTime;
            public uint id;
            public uint syncTag;
            public PackedVec2 pos;
            public PackedVec2 vel;
            public float angle;
            public float angularVelocity;

            public Body(long ownerUid, long lastSnapshotTime, long lastReceivedTime, uint id, uint syncTag, PackedVec2 pos, PackedVec2 vel,
                float angle, float angularVelocity) {
                this.ownerUid = ownerUid;
                this.lastSnapshotTime = lastSnapshotTime;
                this.lastReceivedTime = lastReceivedTime;
                this.id = id;
                this.syncTag = syncTag;
                this.pos = pos;
                this.vel = vel;
                this.angle = angle;
                this.angularVelocity = angularVelocity;
            }

            /// <summary>
            /// create a lifetime update from the last snapshot of this body
            /// </summary>
            /// <param name="body"></param>
            /// <param name="msg"></param>
            public static void copyTo(Body body, BodyLifetimeUpdateMessage msg) {
                msg.time = body.lastSnapshotTime;
                msg.sourceUid = body.ownerUid;
                msg.bodyId = body.id;
                msg.syncTag = body.syncTag;
                msg.exists = true;
            }
        }

        public Dictionary<NetPlayer, List<Body>> bodies = new Dictionary<NetPlayer, List<Body>>();
    }
}