#pragma warning disable 1591

namespace SharpTox.Core
{
    public class ToxGroup
    {
        public int Number { get; private set; }

        public ToxGroupMember[] Members { get; internal set; }

        public ToxGroup(int groupnumber)
        {
            Number = groupnumber;
        }
    }
}

#pragma warning restore 1591