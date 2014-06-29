#pragma warning disable 1591

using System.Collections.ObjectModel;

namespace SharpTox.Core
{
    public class ToxGroup
    {
        public int Number { get; private set; }

        public ReadOnlyCollection<ToxGroupMember> Members { get; internal set; }

        internal ToxGroup(int groupnumber)
        {
            Number = groupnumber;
        }
    }
}

#pragma warning restore 1591