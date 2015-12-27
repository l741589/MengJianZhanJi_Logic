using Assets.data;
using Assets.net;
using Assets.server;
using Assets.utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using T = Assets.data.Types;

namespace Assets.server {
    
    public class DyingState : State {
        private int user;

        public DyingState(int user) {
            this.user = user;
        }

        public override object Run() {
            ActionDesc a=RunSub(new AskForCardCircleAll(user,CardFace.CF_XiuLi)) as ActionDesc;
            if (a.ActionType == ActionType.AT_REFUSE) {
                return new DeadState(user);
            } else {
                return new AlterHpState(user, 1);
            }
        }
    }

    public class DeadState : State {
        private int user;

        public DeadState(int user) {
            this.user = user;
        }

        public override object Run() {
            var us = Status.UserStatus[user];
            us.State = UserStatus.UserState.DEAD;
            GiveUpFlagShip(us);
            us.Group = -1;
            DropCard(us, us.Cards.List);
            Broadcast(new ActionDesc(ActionType.AT_DEAD) { User = user });            
            int alive = -1;
            foreach (var u in Status.UserStatus) {
                if (u.IsDead) continue;
                if (alive == -1) alive = u.Index;
                else return null;
            }
            if (alive != -1) return new WinState(new int[] { alive }.ToList());
            else return null;
        }
    }

    public class WinState : ParentState {
        
        private class _WinState : State{

            private List<int> user;

            public _WinState(List<int> user) {
                this.user = user;
            }

            public override object Run() {
                Broadcast(new ActionDesc(ActionType.AT_WIN) { Users = user });
                return null;
            }
        }

        public WinState(List<int> user) : base(new _WinState(user), 2) { }
    }

    public class AlterHpState : State{

        public int user;
        public int hp;
        public int maxHp;
        public DamageType damageType;

        public AlterHpState(int user, int hp, int maxHp = 0, DamageType damageType = DamageType.NORMAL) {
            this.user = user;
            this.hp = hp;
            this.maxHp = maxHp;
            this.damageType = damageType;
        }

        public override object Run() {
            var u=Status.UserStatus[user];
            u.Hp+=hp;
            u.MaxHp += maxHp;
            if (u.Hp>u.MaxHp) u.Hp=u.MaxHp;
            Broadcast(new ActionDesc(ActionType.AT_ALTER_HP) {
                User = user,
                Arg1 = hp,
                Arg2 = maxHp,
                Group=(int)damageType
            });
            if (Status.UserStatus[user].Hp == 0) {
                return new DyingState(user);
            }
            return null;
        }
    }
    public partial class State {
        public void AlterHp(int user, int delta, DamageType damageType=DamageType.NORMAL) {
            RunSub(new AlterHpState(user, delta, 0, damageType));
        }
    }

    public class PickCardState : State {

        private PrivateList<int> cards;
        private int user;
        private bool hide;

        public PickCardState(int user, List<int> cards, bool hideCards = false) {
            this.user = user;
            this.cards = cards;
            hide = hideCards;
        }

        public override object Run() {
            int card = -1;
            Server.Request(new MessageContext(Clients[user], new ActionDesc(ActionType.AT_ASK_PICK_CARD) {
                User = user,
                Cards = cards.Clone(hide)
            }) {
                Handler = c => {
                    var a = c.GetRes<ActionDesc>();
                    if (c.Client.Index != user) return;
                    card = a.Card;
                }
            });
            if (!cards.Remove(card)){
                Server.Request(Clients[user], T.Action, new ActionDesc(ActionType.AT_REFUSE) { Message = "无效的选择" });
                return this;
            }
            var ret= new ActionDesc(ActionType.AT_PICK_CARD) {
                User = user,
                Card = card,
            };
            if (!hide) Broadcast(ret);
            ret.Cards = cards;
            Result = ret;
            return null;
        }
    }


    public partial class State {
        public ActionDesc RawRoll(int user = -1, int min = 1, int max = 6) {
            if (user == -1) user = Status.Turn;
            var a = new ActionDesc(ActionType.AT_ROLL) { User = user, Arg1 = Random.Next(min, max + 1) };
            Broadcast(a);
            return a;
        }

        public int Roll(int user = -1, int min = 1, int max = 6) {
            return RawRoll(user, min, max).Arg1;
        }

        public ActionDesc RawAskForNumber(int user, int min, int max, string msg = null) {
            SyncStatus();
            var c = PublicRequest(new ActionDesc(ActionType.AT_ASK_NUMBER) {
                User = user,
                Arg1 = min,
                Arg2 = max,
                Message = msg
            });
            ActionDesc a = c.GetRes<ActionDesc>();
            if (a == null || a.ActionType != ActionType.AT_NUMBER) a = new ActionDesc(ActionType.AT_NUMBER) { Arg1 = min };
            Broadcast(a);
            return a;
        }

        public int AskForNumber(int user, int min, int max, string msg = null) {
            
            var a = RawAskForNumber(user, min, max, msg);
            return a.Arg1;
        }

        public int[] DrawCard(UserStatus user, int count = 1, bool isPublic = false) {
            int[] ret = new int[count];
            for (int i = 0; i < count; ++i) {
                if (Status.Stack.IsEmpty()) Shuffle();
                ret[i] = Status.Stack.List.First();
                Status.Stack.List.RemoveFirst();
            }
            if (user != null) user.Cards.AddRange(ret);
            PrivateList<int> cards = ret.ToList();
            if (isPublic) {
                Broadcast(new ActionDesc(ActionType.AT_DRAW_CARD) {
                    User = user.Index,
                    Cards = cards,
                    Arg1 = 1
                });
            } else {
                BatchRequest(c => new server.MessageContext(c, new ActionDesc(ActionType.AT_DRAW_CARD) {
                    User = user.Index,
                    Cards = cards.Clone(c.ClientInfo.Index != user.Index),
                    Arg1 = 0
                }));
            }
            return ret;
        }
        public void DropCard(UserStatus user, IEnumerable<int> cards) {
            if (cards == null) return;
            foreach (var i in cards) user.Cards.List.Remove(i);
            ActionDesc a = new ActionDesc(ActionType.AT_DROP_CARD) {
                User = user.Index,
                Cards = cards.ToList()
            };
            Broadcast(a);
        }
        public ActionDesc RawAskDropCard(UserStatus user, int count = -1) {
            if (count == -1) count = user.Cards.Count - user.Hp;
            if (count <= 0) return null;
            SyncStatus();            
            var c = Server.Request(CurrentClient, T.Action, new ActionDesc(ActionType.AT_ASK_DROP_CARD) { User = Status.Turn, Arg1 = count });
            var a = c.GetRes<ActionDesc>(0);
            return a;
        }

        public List<int> AskDropCard(UserStatus user, int count = -1) {
            var a = RawAskDropCard(user, count);
            if (a == null) return new List<int>();
            return a.Cards.List;
        }

        public List<int> AskWithDropCard(UserStatus user, int count = -1) {
            var a = RawAskDropCard(user, count);
            if (a == null) return new List<int>();
            var list=a.Cards.List;
            DropCard(user,list);
            return list;
        }
    }
}
