using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Net.Data {


    public static class Types {
        public const string GameStart = "GameStart";
        public const string PickRole = "PickRole";
        public const string InitHandCards = "InitHandCards";
    }

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

        public UserStatus() {

        }

        public UserStatus(GameLogic.UserStatus r,bool hidePrivate) {
            Role = r.Role;
            Index = r.Index;
            Camp = r.Camp;
            if (!hidePrivate) {
                Cards = new List<int>(r.Cards);
            }
            CardCount = r.Cards.Count();
            Equip = new List<int>(r.Equip);
            Buff = new List<int>(r.Buff);
        }
    }

    
    class NewCards {

    }
}
