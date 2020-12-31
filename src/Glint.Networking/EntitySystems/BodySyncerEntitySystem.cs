using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Glint.Networking.Components;
using Glint.Networking.Game;
using Glint.Networking.Game.Updates;
using Glint.Networking.Messages;
using Glint.Networking.Utils;
using Glint.Util;
using Nez;

namespace Glint.Networking.EntitySystems {
    /// <summary>
    /// automatically synchronizes from GameSyncer message queues to local entities
    /// </summary>
    public class RemoteBodySyncerSystem : EntitySystem {
        private readonly GameSyncer syncer;
        public Func<string, uint, Entity?> createSyncedEntity;
        public const string SYNC_PREFIX = "_sync";

        private Dictionary<SyncBody, KinStateCache> cachedKinStates = new Dictionary<SyncBody, KinStateCache>();

        public RemoteBodySyncerSystem(GameSyncer syncer, Matcher matcher) :
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

            synchronizeEntities(entities);

            processBodyUpdates(entities);

            updateInterpolations();
        }

        private SyncBody? findBodyById(List<Entity> entities, uint bodyId) {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < entities.Count; i++) {
                var nt = entities[i];
                if (nt.IsDestroyed) continue;

                var body = nt.GetComponent<SyncBody>();
                if (body == null) continue;

                if (body.bodyId == bodyId)
                    return body;
            }

            return null;
        }

        private void synchronizeEntities(List<Entity> entities) {
            var orphanEntities = new List<Entity>();

            foreach (var entity in entities) {
                var body = entity.GetComponent<SyncBody>();
                if (body == null) continue; // skip any entities without body

                // skip any entities that are remote
                if (entity.Name.StartsWith(SYNC_PREFIX)) { // we don't own this one
                    // make sure this peer still exists, and that the body isn't orphaned
                    if (syncer.peers.All(x => x.uid != body.owner)) {
                        // peer no longer exists!
                        orphanEntities.Add(entity);
                        Global.log.trace($"removing body for nonexistent peer {body.owner}");
                        cachedKinStates.Remove(body); // remove from cache
                    }

                    continue;
                }

                // assert ownership over the entity
                if (body.owner != syncer.uid) {
                    body.owner = syncer.uid;
                    body.sendLifetimeCreated();
                }

                // check if this entity is due for us to send a snapshot of it
                if (Time.TotalTime > body.nextUpdate) {
                    // set timer for next snapshot
                    body.nextUpdate = Time.TotalTime + 1f / syncer.systemUps;

                    // send update message
                    var bodyUpdate = syncer.createGameUpdate<BodyKinematicUpdateMessage>(); // get message instance
                    bodyUpdate.reset();
                    bodyUpdate.createFrom(body);
                    syncer.sendGameUpdate(bodyUpdate);
                }
            }

            // get rid of orphans
            foreach (var entity in orphanEntities) {
                entity.Destroy();
            }
        }

        private void processBodyUpdates(List<Entity> entities) {
            // handle queued updates in message queues
            // Global.log.trace($"== body syncer entity system - update() called, pending: {syncer.bodyUpdates.Count}");
            var remoteBodyUpdates = 0U;
            var localBodyUpdates = 0U;
            while (syncer.incomingBodyUpdates.TryDequeue(out var bodyUpdate)) {
                bool isLocalBodyUpdate = bodyUpdate.sourceUid == syncer.uid;
                // update counters
                if (isLocalBodyUpdate)
                    localBodyUpdates++;
                else
                    remoteBodyUpdates++;
#if DEBUG
                if (syncer.debug) {
                    // dump update type
                    var kind = isLocalBodyUpdate ? "LOCAL" : "REMOTE";
                }
#endif

                if (isLocalBodyUpdate) {
                    // this is for resolving desyncs from an authoritative update
                    // TODO: confirm local bodies with local body updates
                    // continue;
                }

                var timeNow = NetworkTime.time();
                var timeOffsetMs = timeNow - bodyUpdate.time;
                if (timeOffsetMs < 0) {
                    // we're getting updates from the future? log in relative time
                    Global.log.warn(
                        $"received an update {timeOffsetMs}ms in the future (current: {NetworkTime.timeSinceStart})," +
                        $" (frame {bodyUpdate.time - NetworkTime.startTime}). perhaps system clocks are out of sync?");
                }

                switch (bodyUpdate) {
                    case BodyKinUpdate kinUpdate: {
                        // find corresponding body
                        var body = findBodyById(entities, bodyUpdate.bodyId);
                        if (body == null) {
                            // we received an update of something we don't know of
                            Global.log.warn(
                                $"received a kinematic update for an unknown body (id {bodyUpdate.bodyId})");
                            break;
                        }

                        if (body.interpolationType == SyncBody.InterpolationType.None)
                            // if no interpolation, then immediately apply the update
                            bodyUpdate.applyTo(body);
                        else if (isLocalBodyUpdate) {
                            // we do a sanity check to make sure our local entity isn't desynced
                            var wasDesynced = resolveLocalDesync(body, kinUpdate);
                            if (wasDesynced) {
                                Global.log.trace(
                                    $"resolved desync for body {body}");
                            }
                        }
                        else {
                            // store in cache to be used by interpolator
                            GAssert.Ensure(cachedKinStates.ContainsKey(body));
                            cachedKinStates[body].stateBuf
                                .enqueue(new KinStateCache.StateFrame(kinUpdate, timeNow));
                        }

                        break;
                    }
                    case BodyLifetimeUpdate lifetimeUpdate: {
                        // we can ignore local lifetime updates
                        if (isLocalBodyUpdate) break;

                        if (!lifetimeUpdate.exists) {
                            // the body is supposed to be dead, destroy it
                            var body = findBodyById(entities, bodyUpdate.bodyId);
                            if (body == null) {
                                Global.log.warn(
                                    $"received a lifetime update to destroy an unknown body (id {bodyUpdate.bodyId})");
                                break;
                            }

                            lifetimeUpdate.applyTo(body);
                            // remove snapshot cache
                            cachedKinStates.Remove(body);
                        }
                        else {
                            // let's create our echo entity
                            var syncEntityName = $"{SYNC_PREFIX}_{bodyUpdate.bodyId}";
                            var syncNt = createSyncedEntity(syncEntityName, bodyUpdate.syncTag);
                            if (syncNt == null) {
                                Global.log.err(
                                    $"failed to create synced entity {syncEntityName} with tag {bodyUpdate.syncTag}");
                                continue;
                            }

                            var body = syncNt.GetComponent<SyncBody>();
                            bodyUpdate.applyTo(body); // this is a new body, immediately apply our first update
                            body.Entity = syncNt;
                            body.bodyId = bodyUpdate.bodyId;
                            body.owner = bodyUpdate.sourceUid;
                            entities.Add(syncNt);

                            // update cache
                            cachedKinStates[body] = new KinStateCache();
                        }

                        break;
                    }
                }
            }

#if DEBUG
            if (syncer.debug) {
                // log the count of body updates that happened this frame
                var totalBodyUpdates = localBodyUpdates + remoteBodyUpdates;
                if (totalBodyUpdates > 0) {
                    Global.log.trace(
                        $"processed ({localBodyUpdates} local) and ({remoteBodyUpdates} remote) body updates this frame");
                    // if (totalBodyUpdates >= syncer.incomingBodyUpdates.capacity) {
                    //     Global.log.trace(
                    //         $"body update ring buffer is full ({syncer.incomingBodyUpdates.capacity}), some updates may have been dropped");
                    // }
                }
            }
#endif
        }

        /// <summary>
        /// check if a local body is desynced, if so, resolve
        /// </summary>
        /// <param name="body"></param>
        /// <param name="kinUpdate"></param>
        /// <returns>whether a desync was present</returns>
        private bool resolveLocalDesync(SyncBody body, BodyKinUpdate kinUpdate) {
            // check if we are desynced

            return false; // no desync
        }

        /// <summary>
        /// step all interpolations using cached kinematic states
        /// </summary>
        private void updateInterpolations() {
            foreach (var cachedKinState in cachedKinStates) {
                var body = cachedKinState.Key;
                var cache = cachedKinState.Value;
                var interpDelay = 1;
                if (cache.stateBuf.Count < interpDelay + 1) continue;
                var update0 = cache.stateBuf.peekAt(interpDelay - 1); // previous
                var update1 = cache.stateBuf.peekAt(interpDelay); // most recent

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
                // Global.log.trace($"interpolate T: {interpT} (sincerecv={timeSince}, diff={timeDiff}");

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

        /// <summary>
        ///     automatically create matcher for all SyncBody subclasses
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static Matcher createMatcher(Assembly assembly) {
            var syncBodyTypes = new List<Type>();
            foreach (var type in assembly.DefinedTypes) {
                if (type.IsSubclassOf(typeof(SyncBody))) {
                    syncBodyTypes.Add(type);
                }
            }

            return new Matcher().One(syncBodyTypes.ToArray());
        }
    }
}