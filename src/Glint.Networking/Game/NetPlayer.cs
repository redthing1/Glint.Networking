namespace Glint.Networking.Game {
    public class NetPlayer {
        public long nick;

        public NetPlayer(long nick) {
            this.nick = nick;
        }

        public override string ToString() {
            return $"({nameof(nick)}: {nick})";
        }
    }
}