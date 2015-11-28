using Assets.Data;
using Assets.Net;
using Assets.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Assets.GameLogic {
    public interface IStateEnvironment {
        ClientHandler[] Clients { get; }
        ClientHandler CurrentClient { get; }
        Status Status { get; }
        Server Server { get; }
        Random Random { get; }
        UserStatus CurrentUser { get; }
    }

    public class StateEnvironment : IStateEnvironment {
        public Status Status { get; set; }
        public ClientHandler[] Clients { get; set; }
        public ClientHandler CurrentClient { get { return Clients[Status.Turn]; } }
        public UserStatus CurrentUser { get { return Status.UserStatus[Status.Turn]; } }
        public Server Server { get; set; }
        public Random Random { get; set; }
    }

    public class ParentState : State {

        public State State { get; private set; }
        public int ToDepth { get; private set; }

        public ParentState(State state,int toDepth=int.MaxValue) {
            State = state;
            ToDepth = toDepth;
        }

        

        public override State Run() {
            throw new NotImplementedException();
        }
    }

    public abstract partial class State : IStateEnvironment {
        private IStateEnvironment env;
        public ClientHandler[] Clients { get { return env.Clients; } }
        public ClientHandler CurrentClient { get { return env.CurrentClient; } }
        public Status Status { get { return env.Status; } }
        public Server Server { get { return env.Server; } }
        public Random Random { get { return env.Random; } }
        public State Parent { get; private set; }
        public UserStatus CurrentUser { get { return env.CurrentUser; } }
        public object Result { get; protected set; }
        public ParentState ParentState { get; private set; }

        public abstract State Run();

        public State() { Parent = null; }

        public State(StateEnvironment env) {
            this.env = env;
            Parent = null;
        }

        public State Next() {
            Thread.Sleep(1000);
            String stateName = GetType().Name;
            LogUtils.LogServer(">>>"+this);
            State ret = Run();
            LogUtils.LogServer("<<<" + this);
            if (ParentState != null) {
                if (Depth >= ParentState.ToDepth) {
                    ret=ParentState.State;
                }
            }
            if (ret!=null) ret.Init(this);
            return ret;
        }

        public State Init(State o) {
            env = o.env;
            Parent = o.Parent;
            Result = o.Result;
            return this;
        }

        public object RunSub(State entry,object initResult=null) {
            entry.Init(this);
            entry.Parent = this;
            entry.Result = initResult;
            object result = null;
            var i = entry;
            while (i!=null) {
                var j = i;
                i = j.Next();
                result = j.Result;
                if (i is ParentState) {
                    this.ParentState = i as ParentState;
                    break;
                }
            }
            return result;
            
        }

        private string typename;

        public override string ToString() {
            if (typename==null) typename = GetType().Name;
            if (Parent == null) return typename;
            else return Parent + "." + typename;
        }

        public int Depth {
            get {
                if (Parent == null) return 0;
                return Parent.Depth + 1;
            }
        }
    }

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

        public MessageContext[] BuildContexts(Func<ClientHandler, MessageContext> creator,ResponseHandler handler=null) {
            int l = Clients.Length;
            MessageContext[] cs = new MessageContext[l];
            for (int i = 0; i < l; ++i) {
                cs[i] = creator(Clients[i]);
                if (handler != null) cs[i].handler = handler;
            }
            return cs;
        }

        public void BatchRequest(Func<ClientHandler, MessageContext> creator, ResponseHandler handler = null) {
            Server.Request(BuildContexts(creator,handler));
        }

        public int[] DrawCard(UserStatus user,int count = 1) {
            int[] ret = new int[count];
            for (int i = 0; i < count; ++i) {
                if (Status.Stack.IsEmpty()) WashCards();
                ret[i] = Status.Stack.List.First();
                Status.Stack.List.RemoveFirst();
            }
            user.Cards.AddRange(ret);
            return ret;
        }

        public void WashCards() {
            Status.Stack.Clear();
            foreach (var i in G.Cards.Keys) Status.Stack.List.AddLast(i);
            Mix(Status.Stack.List);
        }

        public void SyncStatus() {
            BatchRequest(client => new MessageContext(client, Types.SyncStatus, Status.Clone(client.ClientInfo.Index)));
        }

        public void Broadcast(Types type,params object[] body) {
            Server.Broadcast(Clients, type, body);
        }

        public void Broadcast(ActionDesc body) {
            Server.Broadcast(Clients, Types.Action, body);
        }
    }
}
