using System;
using System.Collections.Generic;
using System.Linq;
using Glint.Networking.Components;
using Glint.Networking.Game;
using Glint.Networking.Game.Updates;
using Glint.Networking.Messages;
using Glint.Networking.Utils;
using Glint.Util;
using Nez;
using Nez.Tweens;

namespace Glint.Networking.EntitySystems {
    /// <summary>
    /// automatically synchronizes from GameSyncer message queues to local entities
    /// </summary>
    public class BodySyncerEntitySystem : EntitySystem {
        private readonly GameSyncer syncer;
        public Func<string, uint, Entity> createSyncedEntity;
        public const string SYNC_PREFIX = "_sync";

        public BodySyncerEntitySystem(GameSyncer syncer, Matcher matcher) :
            base(matcher) {
            this.syncer = syncer;
            syncer.gamePeerConnected += gamePeerConnected;
            syncer.gamePeerDisconnected += gamePeerDisconnected;
        }

        private void gamePeerDisconnected(NetPlayer peer) {
            // throw new NotImplementedException();
        }

        private void gamePeerConnected(NetPlayer peer) {
            // throw new NotImplementedException();
        }

        protected override void Process(List<Entity> entities) {
            base.Process(entities);

            // wait for connection to be valid
            if (!syncer.connected) return;

            var entitiesToRemove = new List<Entity>();

            foreach (var entity in entities) {
                var body = entity.GetComponent<SyncBody>();
                if (body == null) continue; // skip any entities without body (in case no types are matched)
                // check if entity information needs to be updated
                // we only need to broadcast on entities that we own
                if (entity.Name.StartsWith(SYNC_PREFIX)) {
                    // we don't own this one
                    // make sure this peer still exists
                    if (syncer.peers.All(x => x.uid != body.owner)) {
                        // peer no longer exists!
                        entitiesToRemove.Add(entity);
                        Global.log.trace($"removing body for nonexistent peer {body.owner}");
                    }

                    continue;
                }
                else {
                    // assert ownership
                    body.owner = syncer.uid;
                }

                if (Time.TotalTime > body.nextUpdate) {
                    body.nextUpdate = Time.TotalTime + 1f / syncer.systemUps;

                    // send update message
                    var bodyUpdate = syncer.createGameUpdate<BodyKinematicUpdateMessage>(); // get message instance
                    bodyUpdate.reset();
                    bodyUpdate.createFrom(body);
                    syncer.sendGameUpdate(bodyUpdate);
                }
            }

            foreach (var entity in entitiesToRemove) {
                entity.Destroy();
            }

            var newEntities = new List<Entity>(entities.Except(entitiesToRemove));

            // handle queued updates
            // Global.log.trace($"== body syncer entity system - update() called, pending: {syncer.bodyUpdates.Count}");
            var remoteBodyUpdates = 0U;
            var localBodyUpdates = 0U;
            while (syncer.bodyUpdates.tryDequeue(out var bodyUpdate)) {
#if DEBUG
                if (syncer.debug) {
                    // dump update type
                    var kind = bodyUpdate.sourceUid == syncer.uid ? "LOCAL" : "REMOTE";
                }
#endif

                // for now, don't apply local body updates
                // TODO: confirm local bodies with local body updates
                // this is for resolving desyncs from an authoritative update
                if (bodyUpdate.sourceUid == syncer.uid) {
                    localBodyUpdates++;
                    continue;
                }

                // 1. find corresponding body
                var body = newEntities.Select(x => x.GetComponent<SyncBody>())
                    .SingleOrDefault(x => x.bodyId == bodyUpdate.bodyId);
                var timeNow = NetworkTime.time();
                var timeOffsetMs = timeNow - bodyUpdate.time;
                if (body == null) {
                    // no matching body. for now we should create one
                    var syncEntityName = $"{SYNC_PREFIX}_{bodyUpdate.bodyId}";
                    var syncNt = createSyncedEntity(syncEntityName, bodyUpdate.syncTag);
                    if (syncNt == null) {
                        Global.log.err(
                            $"failed to create synced entity {syncEntityName} with tag {bodyUpdate.syncTag}");
                        continue;
                    }

                    body = syncNt.GetComponent<SyncBody>();
                    bodyUpdate.applyTo(body); // this is a new body, immediately apply our first update
                    body.Entity = syncNt;
                    body.bodyId = bodyUpdate.bodyId;
                    body.owner = bodyUpdate.sourceUid;
                    newEntities.Add(syncNt);
                }
                else {
                    // 2. apply the body update
                    if (timeOffsetMs < 0) {
                        // we're getting updates from the future? log in relative time
                        Global.log.warn(
                            $"received an update {timeOffsetMs}ms in the future (current: {NetworkTime.timeSinceStart}), (frame {bodyUpdate.time - NetworkTime.startTime})");
                    }

                    var timeOffsetSec = timeOffsetMs / 1000f;

                    switch (bodyUpdate) {
                        case BodyKinUpdate kinUpdate: {
                            switch (body.interpolationType) {
                                case SyncBody.InterpolationType.None:
                                    bodyUpdate.applyTo(body); // just apply the update
                                    break;
                                case SyncBody.InterpolationType.Linear:
                                case SyncBody.InterpolationType.Cubic: {
                                    // we set velocity immediately, but we tween the position
                                    // save the current position
                                    var realPos = kinUpdate.pos.unpack();
                                    var realAngle = kinUpdate.angle;
                                    body.velocity = kinUpdate.vel.unpack();
                                    // check existing tween, and cancel
                                    body.cancelTweens();

                                    // guess interpolation delay by frame difference
                                    var interpolationDelay = timeOffsetSec;
#if DEBUG
                                    if (syncer.debug) {
                                        Global.log.trace($"interpolating with delay {interpolationDelay}");
                                    }
#endif

                                    // figure out ease type
                                    var easeType = default(EaseType);
                                    if (body.interpolationType == SyncBody.InterpolationType.Linear) {
                                        easeType = EaseType.Linear;
                                    }
                                    else if (body.interpolationType == SyncBody.InterpolationType.Cubic) {
                                        easeType = EaseType.CubicInOut;
                                    }

                                    body.posTween = body.Tween(nameof(body.pos), realPos, interpolationDelay)
                                        .SetEaseType(easeType);
                                    body.posTween.Start();
                                    body.angleTween = body.Tween(nameof(body.angle), realAngle, interpolationDelay)
                                        .SetEaseType(easeType);
                                    body.angleTween.Start();

                                    break;
                                }
                            }

                            break;
                        }
                        case BodyLifetimeUpdate lifetimeUpdate: {
                            // apply to body
                            lifetimeUpdate.applyTo(body);
                            break;
                        }
                    }
                }

                remoteBodyUpdates++;
            }

#if DEBUG
            if (syncer.debug) {
                var totalBodyUpdates = localBodyUpdates + remoteBodyUpdates;
                if (totalBodyUpdates > 0) {
                    Global.log.trace(
                        $"processed ({localBodyUpdates} local) and ({remoteBodyUpdates} remote) body updates this frame");
                    if (totalBodyUpdates >= syncer.bodyUpdates.capacity) {
                        Global.log.trace(
                            $"body update ring buffer is full ({syncer.bodyUpdates.capacity}), some updates may have been dropped");
                    }
                }
            }
#endif
        }
    }
}