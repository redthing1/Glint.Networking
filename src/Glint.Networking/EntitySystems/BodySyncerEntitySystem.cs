using System;
using System.Collections.Generic;
using System.Linq;
using Glint.Networking.Components;
using Glint.Networking.Game;
using Glint.Networking.Messages;
using Glint.Networking.Utils;
using Glint.Util;
using Nez;
using Nez.Tweens;

namespace Glint.Networking.EntitySystems {
    public class BodySyncerEntitySystem : EntitySystem {
        private readonly GameSyncer syncer;
        private readonly InterpolationType interpolationType;
        public Func<string, uint, Entity> createSyncedEntity;
        public const string SYNC_PREFIX = "_sync";

        public enum InterpolationType {
            None,
            Linear, // linear smoothing
            Cubic, // cubic smoothing
        }

        public BodySyncerEntitySystem(GameSyncer syncer, InterpolationType interpolationType, Matcher matcher) :
            base(matcher) {
            this.syncer = syncer;
            this.interpolationType = interpolationType;
            syncer.gamePeerConnected += gamePeerConnected;
            syncer.gamePeerDisconnected += gamePeerDisconnected;
        }

        private void gamePeerDisconnected(GamePeer obj) {
            // throw new NotImplementedException();
        }

        private void gamePeerConnected(GamePeer obj) {
            // throw new NotImplementedException();
        }

        protected override void Process(List<Entity> entities) {
            base.Process(entities);
            
            // wait for connection to be valid
            if (!syncer.connected) return;

            var entitiesToRemove = new List<Entity>();

            foreach (var entity in entities) {
                var body = entity.GetComponent<SyncBody>();
                // check if entity information needs to be updated
                // we only need to broadcast on entities that we own
                if (entity.Name.StartsWith(SYNC_PREFIX)) {
                    // we don't own this one
                    // make sure this peer still exists
                    if (syncer.peers.All(x => x.uid != body.ownerUid)) {
                        // peer no longer exists!
                        entitiesToRemove.Add(entity);
                        Global.log.writeLine($"removing body for nonexistent peer {body.ownerUid}",
                            GlintLogger.LogLevel.Trace);
                    }

                    continue;
                } else {
                    // assert ownership
                    body.ownerUid = syncer.uid;
                }

                if (Time.TotalTime > body.nextUpdate) {
                    body.nextUpdate = Time.TotalTime + 1f / syncer.systemUps;

                    // send update message
                    var bodyUpdate = syncer.netNode.getMessage<BodyUpdateMessage>(); // get message instance
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
            // Global.log.writeLine($"== body syncer entity system - update() called, pending: {syncer.bodyUpdates.Count}", GlintLogger.LogLevel.Trace);
            var remoteBodyUpdates = 0U;
            var localBodyUpdates = 0U;
            while (syncer.bodyUpdates.tryDequeue(out var bodyUpdate)) {
                // // dump update type
                var kind = bodyUpdate.sourceUid == syncer.uid ? "LOCAL" : "REMOTE";
                Global.log.writeLine($"    > {kind}, frame {bodyUpdate.time - NetworkTime.startTime}", GlintLogger.LogLevel.Trace);

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
                    var syncNt = createSyncedEntity($"{SYNC_PREFIX}_{bodyUpdate.bodyId}", bodyUpdate.syncTag);
                    body = syncNt.GetComponent<SyncBody>();
                    bodyUpdate.applyTo(body); // this is a new body, immediately apply our first update
                    body.Entity = syncNt;
                    body.bodyId = bodyUpdate.bodyId;
                    body.ownerUid = bodyUpdate.sourceUid;
                    newEntities.Add(syncNt);
                } else {
                    // 2. apply the body update
                    if (timeOffsetMs < 0) {
                        // we're getting updates from the future? log in relative time
                        Global.log.writeLine(
                            $"received an update {timeOffsetMs}ms in the future (current: {NetworkTime.timeSinceStart}), (frame {bodyUpdate.time - NetworkTime.startTime})",
                            GlintLogger.LogLevel.Warning);
                    }

                    var timeOffsetSec = timeOffsetMs / 1000f;

                    switch (interpolationType) {
                        case InterpolationType.None:
                            bodyUpdate.applyTo(body); // just apply the update
                            break;
                        case InterpolationType.Linear:
                        case InterpolationType.Cubic: {
                            // we set velocity immediately, but we tween the position
                            // save the current position
                            var realPos = bodyUpdate.pos.unpack();
                            var realAngle = bodyUpdate.angle;
                            body.velocity = bodyUpdate.vel.unpack();
                            // check existing tween, and cancel
                            body.cancelTweens();

                            // guess interpolation delay by frame difference
                            var interpolationDelay = timeOffsetSec;
                            Global.log.writeLine($"interpolating with delay {interpolationDelay}",
                                GlintLogger.LogLevel.Trace);
                            // figure out ease type
                            var easeType = default(EaseType);
                            if (interpolationType == InterpolationType.Linear) {
                                easeType = EaseType.Linear;
                            } else if (interpolationType == InterpolationType.Cubic) {
                                easeType = EaseType.CubicOut;
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
                }

                remoteBodyUpdates++;
            }

            if (localBodyUpdates + remoteBodyUpdates > 0) {
                Global.log.writeLine(
                    $"processed ({localBodyUpdates} local) and ({remoteBodyUpdates} remote) body updates this frame",
                    GlintLogger.LogLevel.Trace);
            }
        }
    }
}