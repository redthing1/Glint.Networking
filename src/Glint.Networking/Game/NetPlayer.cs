namespace Glint.Networking.Game {
    public class NetPlayer {
        public long uid;

        public NetPlayer(long uid) {
            this.uid = uid;
        }

        public override string ToString() {
            return $"({nameof(uid)}: {uid})";
        }
    }
}