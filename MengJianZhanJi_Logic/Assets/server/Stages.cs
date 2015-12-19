﻿using Assets.data;
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
        public override object Run() {
            ChangeStage(RoundStage.RS_PREPARE);
            SyncStatus();
            return new JudgeState();
        }
    }

    public class JudgeState : StageState {
        public override object Run() {
            ChangeStage(RoundStage.RS_JUDGE);
            return new RelationState();
        }
    }

    public class RelationState : StageState {
        public override object Run() {
            ChangeStage(RoundStage.RS_RELATION);
            SyncStatus();
            ActionDesc a = new ActionDesc(ActionType.AT_ASK_RELATION) { User = Status.Turn };
            var c = PublicRequest(a);
            var ra = c.GetRes<ActionDesc>();
            Broadcast(ra);
            if (Do(ra)) return new DrawCardState();
            return new RelationState();
        }

        public bool Do(ActionDesc a) {
            if (a == null || a.ActionType == ActionType.AT_CANCEL) return true;
            switch (a.ActionType) {
            case ActionType.AT_JOIN_GROUP: {
                    if (CurrentUser.Group == a.Group) {
                        Refuse("无效的操作");
                        return false;
                    }
                    if (a.Group == 0) {
                        GiveUpFlagShip(CurrentUser);
                        CurrentUser.Group = 0;
                    } else {
                        if (Status.UserStatus.Count(u => u.Group == a.Group)+1 > Status.UserStatus.Count(u => u.State == UserStatus.UserState.NORMAL) / 2) {
                            Refuse("无效的操作");
                            return false;
                        }
                        var flagShip = FindFlagShip(a.Group).Index;
                        string msg = string.Format("请 {0} 决定是否同意 {1} 加入 第{2}舰队", Clients[flagShip], CurrentClient.ClientInfo, a.Group);
                        var ret = RunSub(new VoteState(msg, flagShip)) as ActionDesc;
                        if (ret.Success) {
                            CurrentUser.Group = a.Group;
                            BroadcastMessage(string.Format("{0} 同意 {1} 加入 第{2}舰队", Clients[flagShip], CurrentClient.ClientInfo, a.Group));
                        } else {
                            BroadcastMessage(string.Format("{0} 不同意 {1} 加入 第{2}舰队", Clients[flagShip], CurrentClient.ClientInfo, a.Group));
                        }
                    }
                    SyncStatus();
                    break;
                }
            case ActionType.AT_SETUP_GROUP: {
                    HashSet<int> set = new HashSet<int>(new int[] { 1, 2, 3, 4 });
                    foreach (var e in Status.UserStatus) set.Remove(e.Group);
                    for (int i = 1; i <= 4; ++i) {
                        if (set.Contains(i)) {
                            CurrentUser.Group = i;
                            CurrentUser.FlagShip = true;
                            BroadcastMessage(string.Format("{0} 建立了 第{1}舰队", CurrentClient, i));
                            SyncStatus();
                            return true;
                        }
                    }
                    Refuse("无法建立更多的舰队");
                    break;
                }
            case ActionType.AT_FIRE_MEMBER: {
                    int user = a.Users[0];
                    int group = CurrentUser.Group;
                    if (Status.UserStatus[user].State != UserStatus.UserState.NORMAL) {
                        Refuse("无效的操作");
                        return false;
                    }
                    var ret = RunSub(new VoteState(GroupUsersIndex(), string.Format("请投票决定是否将 {0} 开除出 第{1}舰队", Clients[user], group))) as ActionDesc;
                    if (ret != null && ret.Success) {
                        Status.UserStatus[user].Group = 0;
                        Status.UserStatus[user].FlagShip = false;
                        BroadcastMessage(string.Format("成功将 {0} 踢出 第{1}舰队", Clients[user], group));
                    } else {
                        BroadcastMessage(string.Format("没有将 {0} 踢出 第{1}舰队", Clients[user], group));
                    }
                    SyncStatus();
                    break;
                }
            case ActionType.AT_INVITE_MEMBER: {
                    a.Group = CurrentUser.Group;
                    int user = a.Users[0];
                    if (Status.UserStatus[user].State != UserStatus.UserState.NORMAL) {
                        Refuse("无效的操作");
                        return false;
                    }
                    if (Status.UserStatus.Count(u => u.Group == a.Group)+1 > Status.UserStatus.Count(u => u.State == UserStatus.UserState.NORMAL) / 2) {
                        Refuse("无效的操作");
                        return false;
                    }
                    var flagShip = FindFlagShip(a.Group).Index;
                    string msg = string.Format("请 {0} 决定是否同意加入 第{1}舰队", Clients[user], CurrentUser.Group);
                    var ret = RunSub(new VoteState(msg, flagShip)) as ActionDesc;
                    if (ret.Success) {
                        Status.UserStatus[user].Group = CurrentUser.Group;
                        BroadcastMessage(string.Format("{0} 同意加入 第{1}舰队", Clients[user], CurrentUser.Group));
                    } else {
                        BroadcastMessage(string.Format("{0} 放弃加入 第{1}舰队", Clients[user], CurrentUser.Group));
                    }
                    SyncStatus();
                    break;
                }
            }
            return true;
        }
    }

    public class DrawCardState : StageState {
        public override object Run() {
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

        public override object Run() {
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
        public override object Run() {
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
        public override object Run() {
            ChangeStage(RoundStage.RS_FINISH);
            do {
                ++Status.Turn;
            } while (CurrentUser.IsDead);
            return new PrepareState();
        }
    }

}