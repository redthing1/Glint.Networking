namespace Glint.Networking.Game.Updates {
    public class ConnectivityUpdate {
        public NetPlayer peer;
        public ConnectionStatus status;

        public ConnectivityUpdate(NetPlayer peer, ConnectionStatus status) {
            this.peer = peer;
            this.status = status;
        }

        public enum ConnectionStatus {
            Connected,
            Disconnected
        }
    }
}