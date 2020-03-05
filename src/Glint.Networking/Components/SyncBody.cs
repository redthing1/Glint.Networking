using Glint.Physics;
using Microsoft.Xna.Framework;
using Nez.Tweens;

namespace Glint.Networking.Components {
    public abstract class SyncBody : KinBody {
        public float nextUpdate = 0;
        public uint bodyId = (uint) Nez.Random.NextInt(int.MaxValue);
        public uint ownerUid;
        public abstract uint syncTag { get; }
        public ITween<Vector2>? posTween;
        public ITween<float>? angleTween;

        public void cancelTweens() {
            if (posTween?.IsRunning() ?? false) {
                posTween.Stop();
            }
            posTween = null;

            if (angleTween?.IsRunning() ?? false) {
                angleTween.Stop();
            }

            angleTween = null;
        }
        
        public override void OnRemovedFromEntity() {
            base.OnRemovedFromEntity();
            cancelTweens();
        }
    }
}