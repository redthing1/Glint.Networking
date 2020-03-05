namespace Glint.Networking.Game.Updates {
    public class ConnectivityUpdate {
        public GamePeer peer;
        public ConnectionStatus status;

        public ConnectivityUpdate(GamePeer peer, ConnectionStatus status) {
            this.peer = peer;
            this.status = status;
        }

        public enum ConnectionStatus {
            Connected,
            Disconnected
        }
    }
}