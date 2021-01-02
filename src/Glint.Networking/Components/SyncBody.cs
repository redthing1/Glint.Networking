using System;
using Glint.Networking.Game;
using Glint.Networking.Pipeline.Messages;
using Glint.Physics;
using Nez;
using Random = Nez.Random;

namespace Glint.Networking.Components {
    public abstract class SyncBody : KinBody {
        /// <summary>
        /// when the body's state will be snapshotted again (controlled by syncer bodyUps)
        /// </summary>
        internal float nextUpdate = 0;

        /// <summary>
        /// the unique identifier of this body
        /// </summary>
        public uint bodyId = (uint) Random.NextInt(int.MaxValue);

        /// <summary>
        /// the uid of the client that owns this body
        /// </summary>
        public long owner { get; internal set; }

        /// <summary>
        /// a custom field (optionally to be used to identify the type of entity this body is part of)
        /// </summary>
        public abstract uint tag { get; }

        /// <summary>
        /// specifies the interpolation method to use when syncing this body's properties
        /// </summary>
        public abstract InterpolationType interpolationType { get; }

        /// <summary>
        /// specifies the fields of this body that are synced
        /// </summary>
        public virtual InterpolatedFields syncedFields { get; } = InterpolatedFields.All;

        public enum InterpolationType {
            None,
            Linear, // linear interpolation
            Hermite, // hermite splines
        }

        [Flags]
        public enum InterpolatedFields : int {
            None = 0,
            Pos = 1 << 0,
            Vel = 1 << 1,
            Angle = 1 << 2,
            AngularVel = 1 << 3,
            PosAngle = Pos | Angle,
            VelAngularVel = Vel | AngularVel,
            All = PosAngle | VelAngularVel,
        }

        private ClientGameSyncer? syncer => Core.Services.GetService<ClientGameSyncer>();

        /// <summary>
        /// whether the body is owned by the local syncer
        /// </summary>
        public bool isLocal {
            get {
                // check if owned by me
                return owner == syncer?.uid;
            }
        }

        internal void sendLifetimeCreated() {
            if (isLocal) {
                // send create signal
                var lifetimeMessage = syncer!.createGameUpdate<BodyLifetimeUpdateMessage>();
                lifetimeMessage.createFrom(this);
                lifetimeMessage.exists = true;
                syncer.sendGameUpdate(lifetimeMessage);
#if DEBUG
                if (syncer.debug) {
                    Global.log.trace($"sent local SyncBody creation {lifetimeMessage}");
                }
#endif
            }
        }

        internal void sendLifetimeDestroyed() {
            if (isLocal) {
                // send destroy signal
                var lifetimeMessage = syncer!.createGameUpdate<BodyLifetimeUpdateMessage>();
                lifetimeMessage.createFrom(this);
                lifetimeMessage.exists = false;
                syncer.sendGameUpdate(lifetimeMessage);
#if DEBUG
                if (syncer.debug) {
                    Global.log.trace($"sent local SyncBody destruction {lifetimeMessage}");
                }
#endif
            }
        }

        public override void OnRemovedFromEntity() {
            sendLifetimeDestroyed();

            base.OnRemovedFromEntity();
        }
    }
}