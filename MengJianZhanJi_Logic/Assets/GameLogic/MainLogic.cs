using Assets.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using T = Assets.Data.Types;
using Data = Assets.Data;
using UnityEngine;
using Assets.Utility;
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
            Status.Stack = new LinkedList<int>();
            Status.Roles = new LinkedList<int>();
            Status.UserStatus = new UserStatus[Clients.Count()];
            for (int i = 0; i < Status.UserStatus.Length; ++i) {
                Status.UserStatus[i] = new UserStatus() {
                    Index = i,
                    Camp = 0,
                    Hp = 3,
                    MaxHp=4,
                    Cards=new List<int>(),
                    Equip = new List<int>(),
                    Buff=new List<int>()
                };
            }
            BatchRequest(client => new MessageContext(client, T.GameStart, client.ClientInfo));
            SyncStatus();
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
                        Status.UserStatus[cx.client.ClientInfo.Index].Role = role;
                        Log(cx.client, "picked role: {0}", role);
                    }
                };
            });
            return new DealState();
        }
    }

    public class DealState : State {
        public override State Run() {
            BatchRequest(client => new MessageContext(client,
                new Data.ActionDesc {
                    ActionType=ActionType.AT_DRAW_CARD,
                    User = client.Index,
                    Cards = DrawCard(Status.UserStatus[client.Index], 4).ToList()
                }));
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
