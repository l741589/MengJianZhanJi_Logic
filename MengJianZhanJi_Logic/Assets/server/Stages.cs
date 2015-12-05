using Assets.data;
using Assets.utility;
using System.Collections.Generic;
using System.Linq;
using T = Assets.data.Types;
namespace Assets.server {
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
            BatchRequest(c => new server.MessageContext(c, new ActionDesc {
                ActionType = ActionType.AT_DRAW_CARD,
                User = Status.Turn,
                Cards = cards.Clone(c.ClientInfo.Index != Status.Turn)
            }));
            return new UseCardState();
        }
    }


    public class UseCardState : StageState {

        public bool Attacked;

        public UseCardState(bool attacked = false) {
            this.Attacked = attacked;
        }

        public override State Run() {
            ChangeStage(RoundStage.RS_MAIN);
            SyncStatus();
            var c = Server.Request(CurrentClient, T.Action, new ActionDesc(ActionType.AT_ASK) {
                User = Status.Turn,
                Arg1 = Attacked ? 1 : 0
            });
            ActionDesc a = c.ResponseBody.Length > 0 ? c.ResponseBody[0] as ActionDesc : null;
            if (a == null || a.ActionType == ActionType.AT_CANCEL) return new DropCardState();
            switch (a.ActionType) {
            case ActionType.AT_USE_CARD:
                RunSub(UsingCardState.Create(a));
                break;
            }
            return new UseCardState(Attacked);
        }
    }

    public class DropCardState : StageState {
        public override State Run() {
            ChangeStage(RoundStage.RS_DROP);
            SyncStatus();
            var c = Server.Request(CurrentClient, T.Action, new ActionDesc(ActionType.AT_ASK_DROP_CARD) { User = Status.Turn });
            var a = c.GetRes<ActionDesc>(0);
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