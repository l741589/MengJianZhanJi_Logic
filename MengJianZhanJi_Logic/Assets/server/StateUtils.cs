using Assets.data;
using Assets.utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.server {
    public partial class State {

        public void Mix<T>(ICollection<T> c) {
            var list = new List<T>(c);
            int n = list.Count() * 100;
            int count = list.Count();
            while (n-- > 0) {
                int l = Random.Next(count);
                var t = list[l];
                list[l] = list[0];
                list[0] = t;
            }
            c.Clear();
            foreach (var e in list) c.Add(e);
        }

        public void Log(ClientHandler c, String fmt, params object[] args) {
            LogUtils.LogServer(c.ToString() + String.Format(fmt, args));
        }

        public MessageContext[] BuildContexts(Func<ClientHandler, MessageContext> creator, ResponseHandler handler = null, Predicate<MessageContext> successCondtion = null) {
            int l = Clients.Length;
            MessageContext[] cs = new MessageContext[l];
            for (int i = 0; i < l; ++i) {
                cs[i] = creator(Clients[i]);
                if (cs[i] == null) continue;
                if (handler != null) cs[i].Handler = handler;
                if (successCondtion != null) cs[i].SuccessCondition = successCondtion;
            }
            return cs;
        }

        public MessageContext PublicRequest(ActionDesc a) {
            MessageContext ret = null;
            BatchRequest(c => new MessageContext(c, a), c => {
                if (c.Client.Index == a.User) ret = c;
            });
            return ret;
        }

        public void Refuse(String message) {
            Server.Request(CurrentClient, Types.Action, new ActionDesc(ActionType.AT_REFUSE) { Message = message });
        }

        public void BroadcastMessage(String message) {
            Broadcast(new ActionDesc(ActionType.AT_MESSAGE) { Message = message });
        }

        public void BatchRequest(Func<ClientHandler, ActionDesc> creator, ResponseHandler handler = null) {
            BatchRequest(c => new MessageContext(c, creator(c)), handler);
        }

        public void BatchRequest(Func<ClientHandler, MessageContext> creator, ResponseHandler handler = null) {
            Server.Request(BuildContexts(creator, handler));
        }

        public void BatchRequestOne(Func<ClientHandler, MessageContext> creator, Predicate<MessageContext> successCondtion = null, ResponseHandler handler = null) {
            Server.RequestOne(BuildContexts(creator, handler, successCondtion), handler);
        }

        

        public void Shuffle() {
            Status.Stack.Clear();
            foreach (var i in G.Cards.Keys) Status.Stack.List.AddLast(i);
            Mix(Status.Stack.List);
            String s = IOUtils.ReadStringFromFile("Cheat.txt");
            if (s == null) return;
            LogUtils.LogServer("出千中");
            String[] ss = s.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var i in ss) {
                int x;
                if (int.TryParse(i, out x)) {
                    if (Status.Stack.List.Remove(x))
                        Status.Stack.List.AddFirst(x);
                }
            }
        }

        
        /// <summary>
        /// 对每个用户执行操作
        /// </summary>
        /// <param name=""></param>
        /// <param name="a">操作，返回true表示提前结束</param>
        /// <param name="group">分组：-2，所有用户；-1，当前用户；其他指定分组</param>
        /// <param name="from">开始点：-1，当前用户；其他指定用户</param>
        /// <param name="step">步长</param>
        /// <param name="skipDead">是否跳过死亡用户</param>
        /// <returns></returns>
        public void CircleCall(Func<UserStatus, bool> a,int group = -2,int from = -1, int step = 1, bool skipDead = true) {
            int c = Status.UserStatus.Count();
            if (from == -1) from = Status.Turn;
            if (group == -1) group = CurrentUser.Group;
            for (int i = from; i < c; i = i + step) {
                var us = Status.UserStatus[i];
                if (group >= 0 && group != us.Group) continue;
                if (us.IsDead && skipDead) continue;
                if (a(us)) return;
            }
            for (int i = 0; i < from; i = i + step) {
                var us = Status.UserStatus[i];
                if (group >= 0 && group != us.Group) continue;
                if (us.IsDead && skipDead) continue;
                if (a(us)) return;
            }
        }

        public void SyncStatus() {
            BatchRequest(client => new MessageContext(client, Types.SyncStatus, Status.Clone(client.ClientInfo.Index)));
        }

        public void Broadcast(Types type, params object[] body) {
            Server.Broadcast(Clients, type, body);
        }

        public void Broadcast(ActionDesc body) {
            Server.Broadcast(Clients, Types.Action, body);
        }

        public List<UserStatus> GroupUsers(int group = -1) {
            if (group == -1) group = CurrentUser.Group;
            return Status.UserStatus.Where(u => u.Group == group).ToList();
        }

        public List<int> GroupUsersIndex(int group = -1) {
            return GroupUsers(group).Select(u => u.Index).ToList();
        }

        public void GiveUpFlagShip(UserStatus user) {
            UserStatus flagship = null;
            user.FlagShip = false;
            foreach (var e in Status.UserStatus) {
                if (e == user) continue;
                if (e.Group != user.Group || e.State != UserStatus.UserState.NORMAL) continue;
                if (flagship == null) flagship = e;
                else if (e.Hp > flagship.Hp) flagship = e;
                else if (e.Cards.Count > flagship.Cards.Count) flagship = e;
                else if (Random.Next() % 2 == 0) flagship = e;
            }
            if (flagship == null) return;
            flagship.FlagShip = true;
        }

        public UserStatus FindFlagShip(int group) {
            return Status.UserStatus.FirstOrDefault(u => u.FlagShip && u.Group == group);
        }
    }
}
