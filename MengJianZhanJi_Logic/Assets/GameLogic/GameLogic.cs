using Assets.Data;
using Assets.Net;
using Assets.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using T = Assets.Data.Types;

namespace Assets.GameLogic {
    public abstract class StageState : State {

        public void ChangeStage(RoundStage stage) {
            Status.Stage = stage;
            Server.Broadcast(Clients, T.ChangeStage, new StageChangeInfo { Turn = Status.Turn, Stage = Status.Stage });
        }
    }

    public class PrepareState : StageState {
        public override State Run() {
            ChangeStage(RoundStage.RS_PREPARE);
            SyncStatus();
            return new JudgeState();
        }
    }

    public class JudgeState : StageState {
        public override State Run() {
            ChangeStage(RoundStage.RS_JUDGE);
            return new RelationState();
        }
    }

    public class RelationState : StageState {
        public override State Run() {
            ChangeStage(RoundStage.RS_RELATION);
            return new DrawCardState();
        }
    }

    public class DrawCardState : StageState {
        public override State Run() {
            ChangeStage(RoundStage.RS_DRAW);
            PrivateList<int> cards = DrawCard(CurrentUser,2).ToList();
            BatchRequest(c => new Net.MessageContext(c, new ActionDesc {
                ActionType=ActionType.AT_DRAW_CARD,
                User = Status.Turn,
                Cards = cards.Clone(c.ClientInfo.Index != Status.Turn) 
            }));
            return new UseCardState();
        }
    }


    public class UseCardState : StageState {

        private bool attacked;

        public UseCardState(bool attacked=false) {
            this.attacked = attacked;
        }

        public override State Run() {
            ChangeStage(RoundStage.RS_MAIN);
            SyncStatus();
            var c = Server.Request(CurrentClient, T.Action, new ActionDesc(ActionType.AT_ASK) { 
                User=Status.Turn,
                Arg1=attacked?1:0 
            });
            ActionDesc a = c.responseBody.Length>0?c.responseBody[0] as ActionDesc:null;
            if (a==null||a.ActionType==ActionType.AT_CANCEL) return new DropCardState();
            switch (a.ActionType) {
            case ActionType.AT_USE_CARD:
                switch (G.Cards[a.Card].Face) {
                case CardFace.CF_JinJi: {
                        if (attacked) {
                            Server.Request(CurrentClient, T.Action, new ActionDesc {
                                ActionType = ActionType.AT_REFUSE,
                                Message = "每回合只能使用一次" + G.Cards[a.Card].Name
                            });
                            return this;
                        }
                        attacked = true;
                        Broadcast(a);
                        LogUtils.Assert(Status.UserStatus[a.User].Cards.Remove(a.Card));
                        var ca = RunSub(new AskForCardState(a.Users[0], new List<int> { CardFace.CF_HuiBi })) as ActionDesc;
                        if (ca != null && ca.Arg1 == 0) {
                            RunSub(new AlterHpState(ca.User, -1));
                        }
                    }break;
                case CardFace.CF_XiuLi: {
                    CurrentUser.Cards.Remove(a.Card);
                    Broadcast(a);
                    RunSub(new AlterHpState(Status.Turn, 1));
                    }break;
                }
                break;
            }
            return new UseCardState(attacked);
        }
    }

    public class DropCardState : StageState {
        public override State Run() {
            ChangeStage(RoundStage.RS_DROP);
            SyncStatus();
            var c=Server.Request(CurrentClient, T.Action, new ActionDesc(ActionType.AT_ASK_DROP_CARD) { User = Status.Turn });
            var a=c.getResponse<ActionDesc>(0);
            if (a.Cards != null) {
                var u=Status.UserStatus[a.User];
                foreach (var i in a.Cards.List) {
                    u.Cards.List.Remove(i);
                }
            }
            Broadcast(a);
            return new RoundFinishState();
        }
    }

    public class RoundFinishState : StageState {
        public override State Run() {
            ChangeStage(RoundStage.RS_FINISH);
            do {
                ++Status.Turn;
            } while (CurrentUser.IsDead);
            return new PrepareState();
        }
    }


    ////////////////////////////////////

    public class DyingState : State {
        private int user;

        public DyingState(int user) {
            this.user = user;
        }

        public override State Run() {
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

        public override State Run() {
            Status.UserStatus[user].IsDead = true;
            Broadcast(new ActionDesc(ActionType.AT_DROP_CARD) {
                User = user,
                Cards = Status.UserStatus[user].Cards
            });
            Status.UserStatus[user].Cards.Clear();
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

            public override State Run() {
                Broadcast(new ActionDesc(ActionType.AT_WIN) { Users = user });
                return null;
            }
        }

        public WinState(List<int> user) : base(new _WinState(user), 2) { }
    }


    ////////////////////////////////////

    public class AlterHpState : State{

        public int user;
        public int hp;
        public int maxHp;

        public AlterHpState(int user, int hp, int maxHp = 0) {
            this.user = user;
            this.hp = hp;
            this.maxHp = maxHp;
        }

        public override State Run() {
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

    public class AskForCardState : State {
        private int user;
        private List<int> card;
        private ActionDesc askResult;

        public AskForCardState(int user, params int[] card)
            : this(user, card.ToList()) {

        }

        public AskForCardState(int user, List<int> card) {
            this.user = user;
            this.card = card;
        }

        public override State Run() {
            if (Status.UserStatus[user].IsDead) {
                Result = new ActionDesc {
                    ActionType = ActionType.AT_REFUSE,
                    Cards = new List<int>(),
                    Message = "已经死亡",
                    Arg1 = 2
                };
                return null;
            }
            SyncStatus();

            var action = new ActionDesc(ActionType.AT_ASK_CARD) {
                User = user,
                Cards = card,
            };
            List<int> copy = new List<int>(card);
            BatchRequest(c => new MessageContext(c, action), cx => {
                if (cx.client.Index == user) {
                    askResult = cx.getResponse<ActionDesc>(0);
                    LogUtils.Assert(askResult != null);
                }
            });
            LogUtils.Assert(askResult != null);
            if (askResult.ActionType != ActionType.AT_USE_CARD) {
                //TODO 有可能的其他操作，比如技能
                goto FAIL;
            }
            if (askResult.Cards == null) goto FAIL;


            foreach (var i in askResult.Cards.List) {
                if (!Utility.Util.RemoveIf(copy, j => G.Cards[i].Face == j)) {
                    goto FAIL;
                }
            }
            if (copy.Count > 0) goto HALF;
        SUCCESS:
            askResult.Arg1 = 1;
            foreach (var i in askResult.Cards.List) Status.UserStatus[askResult.User].Cards.Remove(i);
            Broadcast(askResult);
            askResult.Cards = copy;
            Result = askResult;
            return null;
        HALF:
            return new AskForCardState(user, copy);
        FAIL:
            askResult.Arg1 = 0;
            askResult.Cards = copy;
            Server.Request(Clients[askResult.User], T.Action, new ActionDesc {
                ActionType = ActionType.AT_REFUSE,
                Cards = copy,
                Message = "无效的出牌"
            });
            Result = askResult;
            return null;
        }
    }

    public class AskForCardCircleAll : State{
        private int from;
        private List<int> card;
        public AskForCardCircleAll(int user, params int[] card)
            : this(user, card.ToList()) {

        }
        public AskForCardCircleAll(int from, List<int> card) {
            this.from = from;
            this.card = card;
        }

        public override State Run() {
            for (int i = from; i < Status.UserStatus.Length; ++i) {
                if (Status.UserStatus[i].IsDead) continue;
                if (AskUser(i)) break;
            }
            if (card != null && card.Count > 0) {
                for (int i = 0; i < from; ++i) {
                    if (Status.UserStatus[i].IsDead) continue;
                    if (AskUser(i)) break;
                }
            }
            if (card == null || card.Count == 0) {
                Result = new ActionDesc(ActionType.AT_USE_CARD) {Cards=card};
            }else{
                Result = new ActionDesc(ActionType.AT_REFUSE) { Cards = card };
            }
            return null;
        }

        private bool AskUser(int user){
            ActionDesc a=RunSub(new AskForCardState(user, card)) as ActionDesc;
            card = a.Cards ?? new List<int>();
            if (a.Arg1 == 1) return true;            
            return false;
        }
    }
}
