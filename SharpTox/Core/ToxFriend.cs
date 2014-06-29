#pragma warning disable 1591

namespace SharpTox.Core
{
    public class ToxFriend
    {
        public ToxUserStatus Status { get; internal set; }

        public string StatusMessage { get; internal set; }
        public string Name { get; internal set; }

        public int Number { get; private set; }

        public bool IsOnline { get; internal set; }
        public bool IsTyping { get; internal set; }

        internal ToxFriend(int friendnumber)
        {
            Number = friendnumber;
        }
    }
}

#pragma warning restore 1591