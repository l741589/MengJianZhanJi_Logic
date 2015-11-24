using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Data;

namespace Assets.GameLogic {
    public class UserStatus {
        public int Role;
        public int Index;
        public int Camp;
        public int Hp;
        public readonly List<int> Cards = new List<int>();
        public readonly List<int> Equip = new List<int>();
        public readonly HashSet<int> Buff = new HashSet<int>();
    }

    public class CardInfo {
        public int Id;
        public string Name;
        public int Color;
        public int Value;
        public int Type;

        private string[] colors = {"", "♥","♦","♠","♣"};
        private string[] values = {"", "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "JOKER" };
        public override string ToString() {
            var ret = Name;
            if (Color != 0) ret += colors[Color];
            if (Value != 0) ret += values[Value];
            return "[" + ret + "]";
        }
    }    

    public class Status {
        public readonly LinkedList<int> Stack = new LinkedList<int>();
        public readonly LinkedList<int> Roles = new LinkedList<int>();
        public int ClientCount { get; set; }
        public UserStatus[] UserStatus;
        private int turn;
        public int Turn { get { return turn; } set { turn = value; if (turn >= ClientCount) turn = 0; } }
        public RoundStage Stage;
    }
}
