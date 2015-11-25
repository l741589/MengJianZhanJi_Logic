using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Data {

    [ProtoContract]
    public class RequestHeader {
        [ProtoMember(1)]
        public Types Type { get; set; }
        [ProtoMember(2)]
        public List<String> BodyTypes { get; set; }


        public override string ToString() {
            return "Request: " + Type;
        }
    }

    [ProtoContract]
    public class ResponseHeader {
        [ProtoMember(1)]
        public Types Type { get; set; }
        [ProtoMember(2)]
        public List<String> BodyTypes { get; set; }

        public override string ToString() {
            return "Response: " + Type;
        }
    }

    [ProtoContract]
    public class UserStatus : ICloneable {
        [ProtoMember(1)]
        public int Role;
        [ProtoMember(2)]
        public int Index;
        [ProtoMember(3)]
        public int Camp;
        [ProtoMember(101)]
        public List<int> Cards;
        [ProtoMember(103)]
        public List<int> Equip;
        [ProtoMember(104)]
        public List<int> Buff;

        public UserStatus() {}

        public UserStatus Clone(bool hidePrivate) {
            UserStatus us = Clone() as UserStatus;
            if (hidePrivate) {
                var len = Cards.Count();
                Cards = new int[len].ToList();
            }
            return us;
        }

        public object Clone() {
            return MemberwiseClone();
        }
    }

    [ProtoContract]
    public enum ActionType {
        [ProtoEnum]
        AT_USE_CARD,
        [ProtoEnum]
        AT_DRAW_CARD,
        [ProtoEnum]
        AT_DROP_CARD,
        [ProtoEnum]
        AT_USE_SKILL,
    }

    [ProtoContract]
    public class ActionDesc {
        [ProtoMember(0)]
        public ActionType Type;
        [ProtoMember(1)]
        public int User;
        [ProtoMember(2)]
        public List<int> Users;
        [ProtoMember(3)]
        public int Card;
        [ProtoMember(4)]
        public List<int> Cards;
        [ProtoMember(5)]
        public int Skill;
    }
    

    [ProtoContract]
    public enum CardType {
        [ProtoEnum]
        CT_NONE = 0,
        [ProtoEnum]
        CT_ROLE = 1,
        [ProtoEnum]
        CT_BASE = 2,
        [ProtoEnum]
        CT_EQUIP = 3,
        [ProtoEnum]
        CT_STRATEGY = 4,
        [ProtoEnum]
        CT_BUFF = 5
    }

    [ProtoContract]
    public enum RoundStage {
        [ProtoEnum]
        RS_PREPARE = 0,
        [ProtoEnum]
        RS_JUDGE = 1,
        [ProtoEnum]
        RS_RELATION =2,
        [ProtoEnum]
        RS_DRAW =3,
        [ProtoEnum]
        RS_MAIN =4,
        [ProtoEnum]
        RS_DROP =5,
        [ProtoEnum]
        RS_FINISH =6,
    }

    [ProtoContract]
    public class ClientInfo {
        [ProtoMember(1)]
        public int Version { get; set; }
        [ProtoMember(2)]
        public String Name { get; set; }
        [ProtoMember(3)]
        public int Index { get; set; }

        public ClientInfo() {
            Version = 0x01000000;
        }

        public override string ToString() {
            return "(" + Name + ")";
        }
    }

    [ProtoContract]
    public class StageChangeInfo {
        [ProtoMember(1)]
        public int Turn;
        [ProtoMember(2)]
        public RoundStage Stage;
    }
}
