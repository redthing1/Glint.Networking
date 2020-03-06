# glint networking

deep networking integration for XNez/Glint

## features
- client/server node-based network architecture
    - headless `GlintNetServer` providing a synchronization server
    - `GameSyncer` client to be integrated into the game scene
- automated XNez entity syncing and interpolation
    - `BodySyncerEntitySystem` can be added to a scene to automatically sync all supported components (anything implementing `SyncBody`)

### note: use case and non-authoritative server

while the networking model of glint networking is technically a client-server network with an authoritative server, the server is currently a blind relay for helping keep clients in sync. for any `SyncBody` types, a client reports the positions of its local "owned" entities and the server will relay them to other clients. there is also currently no verification that a client actually owns the entities it reports for.

this particular design has been chosen because of the simplicity it provides both in implementation and consumption of the library; the main feature provided is keeping entity state in sync across instances of the game. its primary use case is also for small multiplayer games, typically co-op, so we do not concern ourselves with the "cheat-protection" issues that arise in larger, competitive multiplayer games.

## known issues

this library still doesn't do too well with collisions (particularly collision resolution) between local and remote entities. to get the best results right now, intersection collisions work fine but collisions that involve one body pushing another are quite glitchy due to lag and interpolation.

## sample code

see [xnez-net](https://git.rie.icu/xdrie/xnez-net) for a sample project demonstrating usage of glint networking.

## lime

the `Lime` library is included as an integral part of glint networking. it is a messaging/node layer inspired by [neutrino](https://github.com/Claytonious/Neutrino) but built on the more solid [lidgren](https://github.com/lidgren/lidgren-network-gen3) library. it can also be used separately without glint networking.
