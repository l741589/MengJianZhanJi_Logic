using Assets.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.GameLogic {
    using T = Assets.Data.Types;
    using Data = Assets.Data;
    using UnityEngine;
    using Util;
    using Data;

    public class MainLogic {

        private ClientHandler[] clients;
        private Server server;
        private Random ran = new Random();
        private Status status;

        public MainLogic(Server server) {
            this.server = server;
        }

        public void Start(ClientHandler[] clients) {
            this.clients = clients;
            for (int i = 0; i < this.clients.Length; ++i) this.clients[i].ClientInfo.Index = i;
            SetupStatus(clients.Count());

            //通知客户端游戏开始
            BatchRequest(client=>new MessageContext(client,T.GameStart,client.ClientInfo));            

            //选择角色
            for (int i = 0x01000001; i <= 0x01000100; ++i) status.Roles.AddLast(i);
            Mix(status.Roles);
            BatchRequest(client => {
                LinkedList<int> ss = new LinkedList<int>();
                for (int i = 0; i < 5; ++i) {
                    ss.AddLast(status.Roles.First());
                    status.Roles.RemoveFirst();
                }
                return new MessageContext(client, T.PickRole, new Data.ListAdapter<int>(ss)) {
                    handler = cx => {
                        int role = cx.client.Recv<Data.TypeAdapter<int>>();
                        status.UserStatus[cx.client.Index].Role = role;
                        Log(cx.client, "picked role: {0}", role);
                    }
                };
            });

            //发牌
            BatchRequest(client => new MessageContext(client,T.InitHandCards,new Data.ListAdapter<int>(DrawCard(4))));

            status.Turn = 0;

            while (true) {
                ChangeStage(RoundStage.RS_PREPARE);
                break;    
            }
        }

        private void ChangeStage(RoundStage stage) {
            status.Stage = stage;
            server.Broadcast(clients, T.ChangeStage, new StageChangeInfo { Turn = status.Turn, Stage = status.Stage });
        }

        
        private void SetupStatus(int count) {
            status = new Status();
            status.ClientCount = clients.Count();
            status.UserStatus = new UserStatus[status.ClientCount];
            for (int i = 0; i < status.ClientCount; ++i) {
                status.UserStatus[i] = new UserStatus() {
                    Index = i,
                    Camp=0
                };
            }
        }
        private int[] DrawCard(int count=1) {
            int[] ret = new int[count];
            for (int i=0;i< count; ++i) {
                if (!status.Stack.GetEnumerator().MoveNext()) WashCards();
                ret[i] = status.Stack.First();
                status.Stack.RemoveFirst();
            }
            return ret;
        }
        private void WashCards() {
            status.Stack.Clear();
            foreach (var i in G.Cards.Keys) status.Stack.AddLast(i);
            Mix(status.Stack);
        }

        private void Mix<T>(ICollection<T> c) {
            var list = new List<T>(c);
            int n = list.Count() * 100;
            int count = list.Count();
            while (n-- > 0) {
                int l = ran.Next(count);
                var t = list[l];
                list[l] = list[0];
                list[0] = t;
            }
            c.Clear();
            foreach (var e in list) c.Add(e);
        }

        private void Log(ClientHandler c, String fmt, params object[] args) {
            LogUtils.LogServer(c.ToString()+ String.Format(fmt, args));
        }

        private MessageContext[] BuildContexts(Func<ClientHandler,MessageContext> creator) {
            int l = clients.Length;
            MessageContext[] cs = new MessageContext[l];
            for (int i = 0; i < l; ++i) cs[i] = creator(clients[i]);
            return cs;
        }

        private void BatchRequest(Func<ClientHandler, MessageContext> creator) {
            server.Request(BuildContexts(creator));
        }


    }
}
