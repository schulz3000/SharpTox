#pragma warning disable 1591

namespace SharpTox.Core
{
    public class ToxGroupMember
    {
        public string Name { get; internal set; }
        public int Number { get; internal set; }

        internal ToxGroupMember(int number) 
        {
            Number = number;
        }
    }
}

#pragma warning disable 1591