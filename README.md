# glint networking

deep networking integration for XNez/Glint

## features
- client/server node-based network architecture
    - headless `GlintNetServer` providing a synchronization server
    - `GameSyncer` client to be integrated into the game scene
- automated XNez entity syncing and interpolation
    - `BodySyncerEntitySystem` can be added to a scene to automatically sync all supported components (anything implementing `SyncBody`)

## sample code

see [xnez-net](https://git.rie.icu/xdrie/xnez-net) for a sample project demonstrating usage of glint networking.

## lime

the `Lime` library is included as an integral part of glint networking. it is a messaging/node layer inspired by [neutrino](https://github.com/Claytonious/Neutrino) but built on the more solid [lidgren](https://github.com/lidgren/lidgren-network-gen3) library. it can also be used separately without glint networking.
