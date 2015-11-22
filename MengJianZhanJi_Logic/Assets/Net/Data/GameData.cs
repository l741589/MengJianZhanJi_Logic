using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Net.Data {
    [ProtoContract]
    public class UserStatus {
        [ProtoMember(1)]
        public int Role;
        [ProtoMember(2)]
        public int Index;
        [ProtoMember(3)]
        public int Camp;
        [ProtoMember(0x101)]
        public List<int> Cards;
        [ProtoMember(0x102)]
        public int CardCount;
        [ProtoMember(0x103)]
        public List<int> Equip;
        [ProtoMember(0x104)]
        public List<int> Buff;
    }
    class NewCards {

    }
}
