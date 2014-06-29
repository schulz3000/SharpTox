#pragma warning disable 1591

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SharpTox.Core
{
    public class ToxGroup
    {
        public int Number { get; private set; }

        private List<ToxGroupMember> members { get; set; }

        public ReadOnlyCollection<ToxGroupMember> Members
        {
            get
            {
                return members.AsReadOnly();
            }
        }

        internal ToxGroup(int groupnumber)
        {
            Number = groupnumber;
        }

        internal void AddMember(ToxGroupMember member)
        {
            if (!members.Contains(member))
                members.Add(member);
        }

        internal void RemoveMember(ToxGroupMember member)
        {
            if (members.Contains(member))
                members.Remove(member);
        }

        public ToxGroupMember GetGroupMemberByNumber(int peernumber)
        {
            return members.Find(p => p.Number == peernumber);
        }
    }
}

#pragma warning restore 1591