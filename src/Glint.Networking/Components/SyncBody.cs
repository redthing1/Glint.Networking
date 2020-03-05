using Glint.Physics;
using Microsoft.Xna.Framework;
using Nez.Tweens;

namespace Glint.Networking.Components {
    public class SyncBody : KinBody {
        public float nextUpdate = 0;
        public uint bodyId = (uint) Nez.Random.NextInt(int.MaxValue);
        public uint ownerUid;
        public ITween<Vector2>? posTween;
        public ITween<float>? angleTween;

        public virtual uint syncTag { get; set; } = 0U;
        public virtual InterpolationType interpolationType { get; set; } = InterpolationType.Linear;
        
        public enum InterpolationType {
            None,
            Linear, // linear smoothing
            Cubic, // cubic smoothing
        }

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