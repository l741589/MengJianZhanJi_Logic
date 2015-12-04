using Assets.Data;
using Assets.Utility;
using System.Collections.Generic;
using System.Linq;
using T = Assets.Data.Types;
namespace Assets.NetServer {
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
            PrivateList<int> cards = DrawCard(CurrentUser, 2).ToList();
            BatchRequest(c => new NetServer.MessageContext(c, new ActionDesc {
                ActionType = ActionType.AT_DRAW_CARD,
                User = Status.Turn,
                Cards = cards.Clone(c.ClientInfo.Index != Status.Turn)
            }));
            return new UseCardState();
        }
    }


    public class UseCardState : StageState {

        private bool attacked;

        public UseCardState(bool attacked = false) {
            this.attacked = attacked;
        }

        public override State Run() {
            ChangeStage(RoundStage.RS_MAIN);
            SyncStatus();
            var c = Server.Request(CurrentClient, T.Action, new ActionDesc(ActionType.AT_ASK) {
                User = Status.Turn,
                Arg1 = attacked ? 1 : 0
            });
            ActionDesc a = c.ResponseBody.Length > 0 ? c.ResponseBody[0] as ActionDesc : null;
            if (a == null || a.ActionType == ActionType.AT_CANCEL) return new DropCardState();
            switch (a.ActionType) {
            case ActionType.AT_USE_CARD:
                switch (G.Cards[a.Card].Face) {
                case CardFace.CF_JinJi:
                    {
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
                    }
                    break;
                case CardFace.CF_XiuLi:
                    {
                        CurrentUser.Cards.Remove(a.Card);
                        Broadcast(a);
                        RunSub(new AlterHpState(Status.Turn, 1));
                    }
                    break;
                case CardFace.CF_UGuoHouQin:
                    {
                        CurrentUser.Cards.Remove(a.Card);
                        Broadcast(a);

                        PrivateList<int> cards = DrawCard(Status.UserStatus[a.User], 2).ToList();
                        BatchRequest(cx => new NetServer.MessageContext(cx, new ActionDesc {
                            ActionType = ActionType.AT_DRAW_CARD,
                            User = Status.Turn,
                            Cards = cards.Clone(cx.ClientInfo.Index != Status.Turn)
                        }));
                    }
                    break;
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
            var c = Server.Request(CurrentClient, T.Action, new ActionDesc(ActionType.AT_ASK_DROP_CARD) { User = Status.Turn });
            var a = c.getResponse<ActionDesc>(0);
            if (a.Cards != null) {
                var u = Status.UserStatus[a.User];
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

}