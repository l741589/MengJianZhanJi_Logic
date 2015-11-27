using Assets.Data;
using Assets.Util;
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
            BatchRequest(c => new Net.MessageContext(c, T.DrawCard, new ActionDesc {
                Type = T.DrawCard,
                User = Status.Turn,
                Cards = cards.Clone(c.ClientInfo.Index != Status.Turn) 
            }));
            return new UseCardState();
        }
    }


    public class UseCardState : StageState {
        public override State Run() {
            ChangeStage(RoundStage.RS_MAIN);
            SyncStatus();
            var c=Server.Request(CurrentClient, T.AskForAction);
            ActionDesc a = c.responseBody.Length>0?c.responseBody[0] as ActionDesc:null;
            if (a==null||a.ActionType==ActionType.AT_FINISH) return new DropCardState();
            switch (a.ActionType) {
            case ActionType.AT_ATTACK:
                Broadcast(T.DispAction, a);
                LogUtils.Assert(Status.UserStatus[a.User].Cards.Remove(a.Card));
                if (!(bool)RunSub(new AskForCardState(a.Users, new List<int> { CardFace.CF_HuiBi}))) {

                }
                break;
            }
            return new UseCardState();
        }
    }

    public class DropCardState : StageState {
        public override State Run() {
            ChangeStage(RoundStage.RS_DROP);
            return new RoundFinishState();
        }
    }

    public class RoundFinishState : StageState {
        public override State Run() {
            ChangeStage(RoundStage.RS_FINISH);
            Thread.Sleep(5000);
            ++Status.Turn;            
            return new PrepareState();
        }
    }


    ////////////////////////////////////

    public class AskForCardState : State {

        private List<int> users;
        private List<int> card;

        public AskForCardState(List<int> users, List<int> card) {
            this.users = users;
            this.card = card;
        }

        public override State Run() {
            SyncStatus();
            BatchRequest(c => new Net.MessageContext(c, T.AskForCard,
                new ActionDesc {
                    User = users.Contains(c.Index) ? c.Index : -1,
                    Users = users,
                    Cards = card
                }), cx => {
                    var a=cx.getResponse<ActionDesc>(0);
                    if (a==null||a.ActionType != ActionType.AT_REPLY_CARD) {
                        Result = false;
                        return;
                    }
                    foreach (var i in card) {
                        if (!Util.Util.RemoveIf(a.Cards.List, j => G.Cards[j].Face == i)) {
                            Result = false;
                            return;
                        }
                    }
                    if (!a.Cards.IsEmpty()) {
                        Result = false;
                        return;
                    }
                    foreach (var i in a.Cards.List) Status.UserStatus[a.User].Cards.Remove(i);
                    Result = true;
                }
            );
            return null;
        }


    }
}
