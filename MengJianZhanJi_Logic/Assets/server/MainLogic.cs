using Assets.net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using T = Assets.data.Types;
using Data = Assets.data;
using UnityEngine;
using Assets.utility;
using Assets.data;
using Assets.server;

namespace Assets.server {



    public class MainState : State {
        public MainState(StateEnvironment env) : base(env) { }

        public override object Run() {
            RunSub(new GameStartState());
            return null;
        }
    }

    public class GameStartState : State {
        public override object Run() {
            for (int i = 0; i < Clients.Length; ++i) Clients[i].ClientInfo.Index = i;
            Status.Stack = new LinkedList<int>();
            Status.Roles = new LinkedList<int>();
            Status.UserStatus = new UserStatus[Clients.Count()];
            for (int i = 0; i < Status.UserStatus.Length; ++i) {
                Status.UserStatus[i] = new UserStatus() {
                    Index = i,
                    Camp = 0,
                    Hp = 3,
                    MaxHp = 4,
                    Cards = new List<int>(),
                    Equip = new List<int>(),
                    Buff = new List<int>()
                };
            }
            BatchRequest(client => new MessageContext(client, T.GameStart, client.ClientInfo));
            SyncStatus();
            return new PickRoleState();
        }
    }

    public class PickRoleState : State {
        public override object Run() {
            for (int i = 0x01000001; i <= 0x01000100; ++i) Status.Roles.AddLast(i);
            Mix(Status.Roles);
            BatchRequest(client => {
                List<int> ss = new List<int>();
                for (int i = 0; i < 5; ++i) {
                    ss.Add(Status.Roles.First());
                    Status.Roles.RemoveFirst();
                }
                return new MessageContext(client, new ActionDesc(ActionType.AT_ASK_PICK_CARD) {
                    User = client.Index,
                    Cards = ss
                });
            }, cx => {
                var a = cx.GetRes<ActionDesc>();
                Status.UserStatus[cx.Client.Index].Role = a.Card;
                Broadcast(a);
                Log(cx.Client, "picked role: {0}", a.Card);
            });
            return new DealState();
        }
    }

    public class DealState : State {
        public override object Run() {
            BatchRequest(client => new MessageContext(client,
                new data.ActionDesc {
                    ActionType = ActionType.AT_DRAW_CARD,
                    User = client.Index,
                    Cards = DrawCard(Status.UserStatus[client.Index], 4).ToList()
                }));
            return new GameLoopState();
        }
    }

    public class GameLoopState : State {
        public override object Run() {
            Status.Turn = 0;
            RunSub(new PrepareState());
            return null;
        }
    }
}
