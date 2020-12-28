using Glint.Networking.Game;
using Glint.Networking.Messages;
using Glint.Physics;
using Microsoft.Xna.Framework;
using Nez;
using Nez.Tweens;

namespace Glint.Networking.Components {
    public abstract class SyncBody : KinBody {
        public float nextUpdate = 0;
        public uint bodyId = (uint) Nez.Random.NextInt(int.MaxValue);
        public long owner;
        public ITween<Vector2>? posTween;
        public ITween<float>? angleTween;

        public abstract uint bodyType { get; }
        public abstract InterpolationType interpolationType { get; }

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

            var syncer = Core.Services.GetService<GameSyncer>();
            // check if owned by me
            if (owner == syncer?.uid) {
                // send destroy signal
                var lifetimeMessage = syncer.createGameUpdate<BodyLifetimeUpdateMessage>();
                lifetimeMessage.createFrom(this);
                lifetimeMessage.exists = false;
                syncer.sendGameUpdate(lifetimeMessage);
            }
        }
    }
}