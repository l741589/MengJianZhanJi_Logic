using Assets.Data;
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
            Server.Request(CurrentClient, T.DrawCard, new ListAdapter<int>(DrawCard(2)));
            return new UseCardState();
        }
    }


    public class UseCardState : StageState {
        public override State Run() {
            ChangeStage(RoundStage.RS_MAIN);
            //Server.Request(CurrentClient, T.AskForAction);
            return new DropCardState();
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



}
