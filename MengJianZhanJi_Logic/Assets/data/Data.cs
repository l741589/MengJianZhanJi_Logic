using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.data {

    [ProtoContract]
    public class RequestHeader {
        private static long sidCounter = 0;

        [ProtoMember(1)]
        public Types Type { get; set; }
        [ProtoMember(2)]
        public List<String> BodyTypes { get; set; }
        [ProtoMember(3, IsRequired = true)]
        public long Time = DateTime.Now.Ticks;
        [ProtoMember(4, IsRequired = true)]
        public long Sid = sidCounter++;
        
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
        [ProtoMember(3, IsRequired=true)]
        public long Time = DateTime.Now.Ticks;
        [ProtoMember(4, IsRequired = true)]
        public long Sid { get; set; }

        public override string ToString() {
            return "Response: " + Type;
        }
    }

    [ProtoContract]
    public class EquipmentDock {
        [ProtoMember(1, IsRequired = true)]
        public int AntiAir = 0;
        [ProtoMember(2, IsRequired = true)]
        public int Aircraft = 0;
        [ProtoMember(3, IsRequired = true)]
        public int Radar = 0;
        [ProtoMember(4, IsRequired = true)]
        public int Strenthen = 0;

        public bool IsEmpty() {
            return AntiAir == 0 && Aircraft == 0 && Radar == 0 && Strenthen == 0;
        }
    }


    [ProtoContract]
    public class UserStatus : ICloneable {
        [ProtoContract]
        public enum UserState { NORMAL, HIDE, DEAD }

        [ProtoMember(1)]
        public int Role;
        [ProtoMember(2)]
        public int Index;
        [ProtoMember(3)]
        public int Camp;
        [ProtoMember(5)]
        public int Hp { get; set; }
        [ProtoMember(6)]
        public int MaxHp { get; set; }
        [ProtoMember(7, IsRequired = true)]
        public UserState State = UserState.NORMAL;

        [ProtoMember(8, IsRequired = true)]
        public int Group = 0;
        [ProtoMember(9, IsRequired = true)]
        public bool FlagShip = false;

        [ProtoMember(101)]
        public PrivateList<int> Cards;
        [ProtoMember(103, IsRequired = true)]
        public EquipmentDock Equip = new EquipmentDock();
        [ProtoMember(104)]
        public List<int> Buff;

        public UserStatus() { }

        public UserStatus Clone(bool hidePrivate) {
            UserStatus us = Clone() as UserStatus;
            us.Cards = Cards.Clone(hidePrivate);
            return us;
        }

        public object Clone() {
            return MemberwiseClone();
        }

        public bool IsDead { get { return State == UserState.DEAD; } }
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
    public enum DamageType {
        NORMAL = 0,
        AIR = 1,
        TORPEDO = 2
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
