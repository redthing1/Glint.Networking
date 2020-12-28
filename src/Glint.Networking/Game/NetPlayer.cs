namespace Glint.Networking.Game {
    public class NetPlayer {
        public long uid;
        public long lastMessage;
        public string nick;

        public NetPlayer(long uid, string nick) {
            this.uid = uid;
            this.nick = nick;
        }

        public override string ToString() {
            return $"({nameof(uid)}: {uid}, {nameof(lastMessage)}: {lastMessage}, {nameof(nick)}: {nick})";
        }
    }
}