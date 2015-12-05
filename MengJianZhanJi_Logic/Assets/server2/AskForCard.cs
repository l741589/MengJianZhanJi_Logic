using Assets.data;
using Assets.utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using T = Assets.data.Types;
namespace Assets.server {
    public class AskForCardState : State {
        protected int user;
        protected List<int> card;
        protected ActionDesc askResult;

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
            
            DoRequest();
            LogUtils.Assert(askResult != null);
            List<int> copy = new List<int>(card);
            if (askResult.ActionType != ActionType.AT_USE_CARD) {
                //TODO 有可能的其他操作，比如技能
                return OnFail(copy);
            }

            if (askResult.Cards == null) return OnFail(copy);


            foreach (var i in askResult.Cards.List) {
                if (!utility.Util.RemoveIf(copy, j => G.Cards[i].Face == j)) {
                    return OnFail(copy);
                }
            }
            if (copy.Count > 0) return OnHalf(copy);
            return OnSuccess(copy);
        }

        public virtual State OnSuccess(List<int> copy){
            askResult.Arg1 = 1;
            foreach (var i in askResult.Cards.List) Status.UserStatus[askResult.User].Cards.Remove(i);
            Broadcast(askResult);
            askResult.Cards = copy;
            Result = askResult;
            return null;
        }

        public virtual State OnHalf(List<int> copy) {
            foreach (var i in askResult.Cards.List) Status.UserStatus[askResult.User].Cards.Remove(i);
            card = copy;
            askResult = null;
            return this;
        }

        public virtual State OnFail(List<int> copy){
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

        public virtual void DoRequest(){
            var action = new ActionDesc(ActionType.AT_ASK_CARD) {
                User = user,
                Cards = card,
            };
            BatchRequest(c => new MessageContext(c, action), cx => {
                if (cx.Client.Index == user) {
                    askResult = cx.GetRes<ActionDesc>(0);
                    LogUtils.Assert(askResult != null);
                }
            });
        }
    }

    public class AskForCardSyncAll : AskForCardState {

        public AskForCardSyncAll(int user, params int[] card)
            : base(user, card) {

        }

        public AskForCardSyncAll(int user, List<int> card)
            : base(user, card) {

        }

        public override void DoRequest() {
            BatchRequestOne(c => new MessageContext(c, new ActionDesc(ActionType.AT_ASK_CARD) {
                User = user,
                Cards = card
            }), c => {
                if (c.ResponseHeader.Type != T.Action) return;
                askResult = c.GetRes<ActionDesc>();
            });
        }

        public override State OnSuccess(List<int> copy) {
            return base.OnSuccess(copy);
        }
    }

    public class AskForCardCircleAll : State {
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
                Result = new ActionDesc(ActionType.AT_USE_CARD) { Cards = card };
            } else {
                Result = new ActionDesc(ActionType.AT_REFUSE) { Cards = card };
            }
            return null;
        }

        private bool AskUser(int user) {
            ActionDesc a = RunSub(new AskForCardState(user, card)) as ActionDesc;
            card = a.Cards ?? new List<int>();
            if (a.Arg1 == 1) return true;
            return false;
        }
    }
}
