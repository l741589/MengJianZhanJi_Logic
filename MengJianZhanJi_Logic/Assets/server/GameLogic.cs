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
            Broadcast(new ActionDesc(ActionType.AT_DROP_CARD) {
                User = user,
                Cards = us.Cards
            });
            us.Cards.Clear();
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

        public AlterHpState(int user, int hp, int maxHp = 0) {
            this.user = user;
            this.hp = hp;
            this.maxHp = maxHp;
        }

        public override object Run() {
            var u=Status.UserStatus[user];
            u.Hp+=hp;
            u.MaxHp += maxHp;
            if (u.Hp>u.MaxHp) u.Hp=u.MaxHp;
            Broadcast(new ActionDesc(ActionType.AT_ALTER_HP) {
                User = user,
                Arg1 = hp,
                Arg2 = maxHp
            });
            if (Status.UserStatus[user].Hp == 0) {
                return new DyingState(user);
            }
            return null;
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
}
