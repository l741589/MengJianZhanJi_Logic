using Assets.Data;
using Assets.Net;
using Assets.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.GameLogic {
    public interface IStateEnvironment {
        ClientHandler[] Clients { get; }
        ClientHandler CurrentClient { get; }
        Status Status { get; }
        Server Server { get;  }
        Random Random { get; }
    }

    public class StateEnvironment : IStateEnvironment {
        public Status Status { get; set; }
        public ClientHandler[] Clients { get; set; }
        public ClientHandler CurrentClient { get { return Clients[Status.Turn]; } }
        public Server Server { get; set; }
        public Random Random { get; set; }
    }

    public abstract partial class State : IStateEnvironment {
        private IStateEnvironment env;
        public ClientHandler[] Clients { get { return env.Clients; } }
        public ClientHandler CurrentClient { get { return env.CurrentClient; } }
        public Status Status { get { return env.Status; } }
        public Server Server { get { return env.Server; } }
        public Random Random { get { return env.Random; } }
        public State Parent { get; private set; }

        public abstract State Run();

        public State() { Parent = null; }

        public State(StateEnvironment env) {
            this.env = env;
            Parent = null;
        }

        public State Next() {
            String stateName = GetType().Name;
            LogUtils.LogServer(">>>"+this);
            State ret = Run();
            LogUtils.LogServer("<<<" + this);
            if (ret!=null) ret.Init(this);
            return ret;
        }

        public State Init(State o) {
            env = o.env;
            Parent = o.Parent;
            return this;
        }

        public void RunSub(State entry) {
            entry.Init(this);
            entry.Parent = this;
            for (var i =  entry; i != null; i = i.Next()) ;
        }

        private string typename;

        public override string ToString() {
            if (typename==null) typename = GetType().Name;
            if (Parent == null) return typename;
            else return Parent + "." + typename;
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

        public MessageContext[] BuildContexts(Func<ClientHandler, MessageContext> creator) {
            int l = Clients.Length;
            MessageContext[] cs = new MessageContext[l];
            for (int i = 0; i < l; ++i) cs[i] = creator(Clients[i]);
            return cs;
        }

        public void BatchRequest(Func<ClientHandler, MessageContext> creator) {
            Server.Request(BuildContexts(creator));
        }

        public int[] DrawCard(int count = 1) {
            int[] ret = new int[count];
            for (int i = 0; i < count; ++i) {
                if (!Status.Stack.GetEnumerator().MoveNext()) WashCards();
                ret[i] = Status.Stack.First();
                Status.Stack.RemoveFirst();
            }
            return ret;
        }

        public void WashCards() {
            Status.Stack.Clear();
            foreach (var i in G.Cards.Keys) Status.Stack.AddLast(i);
            Mix(Status.Stack);
        }
    }
}
