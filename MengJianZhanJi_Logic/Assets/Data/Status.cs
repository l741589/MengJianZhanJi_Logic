using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Data;
using ProtoBuf;

namespace Assets.Data {

    public class CardInfo {
        public int Id;
        public string Name;
        public int Color;
        public int Value;
        public int Type;
        public int Face;
        public string Symbol;

        private string[] colors = {"", "♥","♦","♠","♣"};
        private string[] values = {"", "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "JOKER" };
        public override string ToString() {
            var ret = Name;
            if (Color != 0) ret += colors[Color];
            if (Value != 0) ret += values[Value];
            return "[" + ret + "]";
        }

        public static string ToString(int id) {
            return G.Cards[id].ToString();
        }
    }

    [ProtoContract]
    public class Status : ICloneable{

        [ProtoMember(1)]
        public PrivateLinkedList<int> Stack;
        public LinkedList<int> Roles;

        [ProtoMember(2)]
        public UserStatus[] UserStatus;

        [ProtoMember(3)]
        public int Turn { get { return turn; } set { turn = value; if (turn >= UserStatus.Length) turn = 0; } }
        private int turn;

        [ProtoMember(4)]
        public RoundStage Stage;

        

        public Status Clone(int hide = -1) {
            Status us = (this as ICloneable).Clone() as Status;
            if (hide < 0) return us;
            us.Stack = Stack==null?null:Stack.Clone(true);
            us.Roles = null;
            us.UserStatus = UserStatus.Select(i => i.Clone(i.Index != hide)).ToArray();
            return us;
        }

        object ICloneable.Clone() {
            return MemberwiseClone();
        }
    }
}
