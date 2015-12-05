﻿using ProtoBuf;
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
    public class UserStatus : ICloneable {
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
        public bool IsDead = false;
        [ProtoMember(101)]
        public PrivateList<int> Cards;
        [ProtoMember(103)]
        public List<int> Equip;
        [ProtoMember(104)]
        public List<int> Buff;
        
        public UserStatus() {}

        public UserStatus Clone(bool hidePrivate) {
            UserStatus us = Clone() as UserStatus;
            us.Cards = Cards.Clone(hidePrivate);
            return us;
        }

        public object Clone() {
            return MemberwiseClone();
        }
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