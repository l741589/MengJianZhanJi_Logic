using Assets.data;
using Assets.net;
using Assets.server;
using Assets.utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Assets.server {
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



        public override object Run() {
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
        public bool IsClosed { get; private set; }
        public State CurrentSub { get; private set;}

        public abstract object Run();

        public State():this(null) {}

        public State(StateEnvironment env) {
            this.env = env;
            Parent = null;
            IsClosed = false;
        }

        public State Next() {
            Thread.Sleep(500);
            String stateName = GetType().Name;
            LogUtils.LogServer(">>>"+this);
            object result = Run();
            State next = null;
            if (result is State) {
                next = result as State;
            } else if (result != null) {
                Result = result;
            }
            LogUtils.LogServer("<<<" + this);
            if (IsClosed) return null;
            if (ParentState != null) {
                if (Depth <= ParentState.ToDepth) {
                    next = ParentState.State;
                } else {
                    next = ParentState;
                }
            }
            if (next != null) next.Init(this);
            return next;
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
                CurrentSub = i;
                i = CurrentSub.Next();
                result = CurrentSub.Result;
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

        public void Close() {
            if (CurrentSub != null) CurrentSub.Close();
            IsClosed = true;
        }
    }
}
