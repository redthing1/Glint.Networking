namespace Glint.Networking.Game {
    public class NetPlayer {
        public long nick;
        public uint uid;

        public NetPlayer(long nick, uint uid) {
            this.nick = nick;
            this.uid = uid;
        }

        public override string ToString() {
            return $"(nick: {nick}, uid: {uid})";
        }
    }
}