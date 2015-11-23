using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    public class Status {
        public readonly LinkedList<int> Stack = new LinkedList<int>();
        public readonly LinkedList<int> Roles = new LinkedList<int>();
        public int ClientCount { get; set; }

        private int turn;
        public int Turn { get { return turn; } set { turn = value; if (turn >= ClientCount) turn = 0; } }
    }
}
