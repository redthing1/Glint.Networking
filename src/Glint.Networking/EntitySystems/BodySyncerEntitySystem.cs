using System;
using System.Collections.Generic;
using System.Linq;
using Glint.Networking.Components;
using Glint.Networking.Game;
using Glint.Networking.Game.Updates;
using Glint.Networking.Messages;
using Glint.Networking.Utils;
using Glint.Networking.Utils.Collections;
using Glint.Util;
using Microsoft.Xna.Framework;
using Nez;
using Nez.Tweens;

namespace Glint.Networking.EntitySystems {
    /// <summary>
    /// automatically synchronizes from GameSyncer message queues to local entities
    /// </summary>
    public class BodySyncerEntitySystem : EntitySystem {
        private readonly GameSyncer syncer;
        public Func<string, uint, Entity?> createSyncedEntity;
        public const string SYNC_PREFIX = "_sync";

        private Dictionary<SyncBody, KinStateCache> cachedKinStates = new Dictionary<SyncBody, KinStateCache>();

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
                        cachedKinStates.Remove(body); // remove from cache
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

            var livingEntities = new List<Entity>(entities.Except(entitiesToRemove));

            // handle queued updates in message queues
            // Global.log.trace($"== body syncer entity system - update() called, pending: {syncer.bodyUpdates.Count}");
            var remoteBodyUpdates = 0U;
            var localBodyUpdates = 0U;
            while (syncer.bodyUpdates.tryDequeue(out var bodyUpdate)) {
                bool isLocalBodyUpdate = bodyUpdate.sourceUid == syncer.uid;
#if DEBUG
                if (syncer.debug) {
                    // dump update type
                    var kind = isLocalBodyUpdate ? "LOCAL" : "REMOTE";
                }
#endif

                // for now, don't apply local body updates
                // TODO: confirm local bodies with local body updates
                // this is for resolving desyncs from an authoritative update
                if (isLocalBodyUpdate) {
                    localBodyUpdates++;
                    continue;
                }

                // 1. find corresponding body
                var body = livingEntities.Select(x => x.GetComponent<SyncBody>())
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
                    livingEntities.Add(syncNt);

                    // update cache
                    cachedKinStates[body] = new KinStateCache();
                }
                else {
                    // 2. apply the body update
                    if (timeOffsetMs < 0) {
                        // we're getting updates from the future? log in relative time
                        Global.log.warn(
                            $"received an update {timeOffsetMs}ms in the future (current: {NetworkTime.timeSinceStart}), (frame {bodyUpdate.time - NetworkTime.startTime})");
                    }

                    switch (bodyUpdate) {
                        case BodyKinUpdate kinUpdate: {
                            // store in cache
                            GAssert.Ensure(cachedKinStates.ContainsKey(body));
                            cachedKinStates[body].stateBuf.enqueue(new KinStateCache.StateFrame(kinUpdate, timeNow));

                            // set up things for interpolation
                            switch (body.interpolationType) {
                                case SyncBody.InterpolationType.None:
                                    bodyUpdate.applyTo(body); // just apply the update
                                    break;
                                default:
                                    break;
                            }

                            break;
                        }
                        case BodyLifetimeUpdate lifetimeUpdate: {
                            // apply to body
                            lifetimeUpdate.applyTo(body);
                            // if gone, propagate
                            if (!lifetimeUpdate.exists) {
                                cachedKinStates.Remove(body);
                            }

                            break;
                        }
                    }
                }

                remoteBodyUpdates++;
            }

#if DEBUG
            if (syncer.debug) {
                // log the count of body updates that happened this frame
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

            // step all interpolations
            foreach (var cachedKinState in cachedKinStates) {
                var body = cachedKinState.Key;
                var cache = cachedKinState.Value;
                if (cache.stateBuf.Count < 2) continue;
                var update0 = cache.stateBuf.peekAt(0); // previous
                var update1 = cache.stateBuf.peekAt(1); // most recent

                // calculate time discrepancy
                var timeNow = NetworkTime.time();
                // time between stored frames
                var timeDiff = (update1.data.time - update0.data.time) / 1000f;
                // time since we received the most recent frame
                var timeSince = (timeNow - update1.receivedAt) / 1000f;

                // if our interpolation window exceeds the time window we received, skip
                if (timeSince > timeDiff) {
                    continue;
                }

                var interpT = (timeSince / timeDiff); // progress in interpolation

                // // just apply directly
                // update1.applyTo(body);

                switch (body.interpolationType) {
                    case SyncBody.InterpolationType.Linear:
                        body.pos = InterpolationUtil.lerp(update0.data.pos.unpack(), update1.data.pos.unpack(),
                            interpT);
                        body.angle = InterpolationUtil.lerp(update0.data.angle, update1.data.angle, interpT);
                        break;
                    case SyncBody.InterpolationType.Hermite:
                        body.pos = InterpolationUtil.hermite(update0.data.pos.unpack(), update1.data.pos.unpack(),
                            update0.data.vel.unpack() * Time.DeltaTime, update1.data.vel.unpack() * Time.DeltaTime,
                            interpT);
                        body.angle = InterpolationUtil.hermite(update0.data.angle, update1.data.angle,
                            update0.data.angularVelocity * Time.DeltaTime,
                            update1.data.angularVelocity * Time.DeltaTime, interpT);
                        break;
                }
            }
        }
    }
}