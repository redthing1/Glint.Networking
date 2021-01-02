
# glint networking

deep networking integration for XNez+Glint

## features
- client/server node-based network architecture
    - extensible message handler system
    - headless `GlintNetServer` providing a synchronization server
    - `GameSyncer` client to be integrated into the game scene
    - fast, lightweight, efficient binary serialization of messages with customizable reliability and ordering options
- automated XNez entity syncing and interpolation
    - `RemoteBodySyncerSystem` can be added to a scene to automatically sync all supported components (anything implementing `SyncBody`)
    - multiple interpolation methods: one of `None`, `Linear`, `Hermite` can be specified for each type of synced body

### note: use case and non-authoritative server

while the networking model of glint networking is technically a client-server network with an authoritative server, the server is by default a blind relay that only enforces message validity. for any `SyncBody` types, a client reports the positions of its local "owned" entities and the server will relay them to other clients. there is also currently no verification that a client actually owns the entities it reports for. however, the server interface is quite extensible and it would be fairly straightforward to add this feature.

this particular design has been chosen because of the simplicity it provides both in implementation and consumption of the library; the main feature provided is keeping entity state in sync across instances of the game. its primary use case is also for small multiplayer games, typically co-op, so we do not concern ourselves with the "cheat-protection" issues that arise in larger, competitive multiplayer games.

## sample code

see [Lem](https://github.com/xdrie/Lem) for a sample project demonstrating usage of glint networking.

## lime

the `Lime` library is included as an integral part of glint networking. it is a messaging/node layer inspired by [neutrino](https://github.com/Claytonious/Neutrino) but built on the more solid [lidgren](https://github.com/lidgren/lidgren-network-gen3) library. it can also be used separately without glint networking.
