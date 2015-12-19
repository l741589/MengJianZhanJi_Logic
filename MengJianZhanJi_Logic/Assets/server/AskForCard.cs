using Assets.data;
using Assets.utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using T = Assets.data.Types;
namespace Assets.server {
    public class AskForCardState : State {
        public int user;
        public List<int> card;
        public ActionDesc askResult;
        private Predicate<AskForCardState> cancelCond;

        public AskForCardState(int user, params int[] card)
            : this(user, card.ToList()) {

        }

        public AskForCardState(int user, List<int> card) {
            this.user = user;
            this.card = card;
        }

        public AskForCardState Cancelable(Predicate<AskForCardState> cond) {
            cancelCond = cond;
            return this;
        }

        public override object Run() {
            if (user >= 0 && Status.UserStatus[user].IsDead) {
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

            if (askResult.Cards == null || askResult.Cards.Count == 0) return OnFail(copy);


            foreach (var i in askResult.Cards.List) {
                if (!utility.Util.RemoveIf(copy, j => G.Cards[i].Face == j)) {
                    return OnFail(copy);
                }
            }
            if (copy.Count > 0) return OnHalf(copy);
            Apply();
            if (cancelCond==null||!cancelCond(this)) return OnSuccess(copy);            
            return OnCanceled(copy);
        }

        public virtual void Apply() {
            foreach (var i in askResult.Cards.List) Status.UserStatus[askResult.User].Cards.Remove(i);
            Broadcast(askResult);
        }

        public virtual State OnSuccess(List<int> copy) {
            askResult.Success = true;
            askResult.Cards = copy;
            Result = askResult;
            return null;
        }

        public virtual State OnCanceled(List<int> copy) {
            askResult.Success = false;
            askResult.Cards = copy;
            Result = askResult;
            return null;
        }

        public virtual State OnHalf(List<int> copy) {
            Apply();
            card = copy;
            askResult = null;
            return this;
        }

        public virtual State OnFail(List<int> copy){
            askResult.Success = false;
            askResult.Cards = copy;
            if (askResult.User != -1) {
                Server.Request(Clients[askResult.User], T.Action, new ActionDesc {
                    ActionType = ActionType.AT_REFUSE,
                    Cards = copy,
                    Message = "无效的出牌"
                });
            }
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

        public AskForCardSyncAll(params int[] card)
            : base(0, card) {

        }

        public AskForCardSyncAll(List<int> card)
            : base(0, card) {

        }

        public override void DoRequest() {
            BatchRequestOne(c => new MessageContext(c, new ActionDesc(ActionType.AT_ASK_CARD) {
                User = c.Index,
                Cards = card
            }), c => c.Type == T.Action && c.GetRes<ActionDesc>().ActionType == ActionType.AT_USE_CARD,
            c => {
                if (c == null) {
                    askResult = new ActionDesc(ActionType.AT_CANCEL);
                } else {
                    if (c.ResponseHeader.Type != T.Action) return;
                    askResult = c.GetRes<ActionDesc>();
                }
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

        public override object Run() {
            CircleCall(u => AskUser(u.Index));
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
            if (a.Success) return true;
            return false;
        }
    }
}
