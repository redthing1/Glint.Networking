namespace Glint.Networking.Game {
    public class GamePeer {
        public long remId;
        public uint uid;

        public GamePeer(long remId, uint uid) {
            this.remId = remId;
            this.uid = uid;
        }

        public override string ToString() {
            return $"(nick: {remId}, uid: {uid})";
        }
    }
}