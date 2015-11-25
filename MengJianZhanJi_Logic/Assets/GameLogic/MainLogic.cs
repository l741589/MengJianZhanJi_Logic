using Assets.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using T = Assets.Data.Types;
using Data = Assets.Data;
using UnityEngine;
using Assets.Util;
using Assets.Data;
namespace Assets.GameLogic {
  


    public class MainState : State {
        public MainState(StateEnvironment env) : base(env) { }

        public override State Run() {   
            RunSub(new GameStartState());
            return null;
        }
    }

    public class GameStartState : State{
        public override State Run() {
            for (int i = 0; i < Clients.Length; ++i) Clients[i].ClientInfo.Index = i;
            Status.ClientCount = Clients.Count();
            Status.UserStatus = new UserStatus[Status.ClientCount];
            for (int i = 0; i < Status.ClientCount; ++i) {
                Status.UserStatus[i] = new UserStatus() {
                    Index = i,
                    Camp = 0
                };
            }
            BatchRequest(client => new MessageContext(client, T.GameStart, client.ClientInfo));
            return new PickRoleState();
        }
    }

    public class PickRoleState : State {
        public override State Run() {
            for (int i = 0x01000001; i <= 0x01000100; ++i) Status.Roles.AddLast(i);
            Mix(Status.Roles);
            BatchRequest(client => {
                LinkedList<int> ss = new LinkedList<int>();
                for (int i = 0; i < 5; ++i) {
                    ss.AddLast(Status.Roles.First());
                    Status.Roles.RemoveFirst();
                }
                return new MessageContext(client, T.PickRole, new Data.ListAdapter<int>(ss)) {
                    handler = cx => {
                        int role = cx.responseBody[0] as Data.TypeAdapter<int>;
                        Status.UserStatus[cx.client.Index].Role = role;
                        Log(cx.client, "picked role: {0}", role);
                    }
                };
            });
            return new DealState();
        }
    }

    public class DealState : State {
        public override State Run() {
            BatchRequest(client => new MessageContext(client, T.InitHandCards, new Data.ListAdapter<int>(DrawCard(4))));
            return new GameLoopState();
        }
    }

    public class GameLoopState : State {
        public override State Run() {
            Status.Turn = 0;
            RunSub(new PrepareState());
            return null;
        }
    }


    public class MainLogic {

        private Server server;
        private Status status;

        public MainLogic(Server server) {
            this.server = server;
        }

        public void Start(ClientHandler[] clients) {

            StateEnvironment env = new StateEnvironment {
                Clients = clients,
                Random = new Random(),
                Server = server,
                Status = new Status()
            };
            new MainState(env).Next();
        }

     

        
       
        

    }
}
