using Assets.Data;
using Assets.GameLogic;
using Assets.Util;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.Net {
    class Client {
        static Client() {
            InitHanlders();
        }

        private TcpClient TcpClient { get; set; }
        private bool isRunning = true;
        public Socket Sock { get { return TcpClient.Client; } }
        public Data.ClientInfo Info { get; private set; }
        public Status Status { get; private set; }
        public UserStatus MyStatus { get { return Status.UserStatus[Info.Index]; } }

        public delegate void RequestHandler(Client client,Data.RequestHeader header,object[] body);
        private static Dictionary<Data.Types, RequestHandler> requestDispatcher = new Dictionary<Data.Types, RequestHandler>();
        private AutoResetEvent Event = new AutoResetEvent(true);
        private Action<Data.RequestHeader> response;


        public Client(String ip,Data.ClientInfo info) {
            TcpClient = new TcpClient();
            Info=info;
            TcpClient.Connect(ip, NetHelper.Port);
        }

        public void Loop() {
            NetHelper.Send(Sock, Info);
            isRunning = true;
            while (isRunning) {
                try {
                    Data.RequestHeader header = NetHelper.Recv<Data.RequestHeader>(Sock);
                    LogUtils.LogClient(header.ToString());
                    object[] requestBody;
                    var types = header.BodyTypes;
                    if (types != null) {
                        int count = types.Count();
                        requestBody = new object[count];
                        for (int i = 0; i < count; ++i) {
                            Type type = Type.GetType(types[i]);
                            requestBody[i] = NetHelper.Recv(Sock, type);
                        }
                    } else {
                        requestBody = new object[0];
                    }
                    RequestHandler a;
                    if (requestDispatcher.TryGetValue(header.Type, out a)) {
                        a(this, header,requestBody);
                        Event.WaitOne();
                        if (!isRunning) break;
                        if (response != null) {
                            response(header);
                            response = null;
                        }
                    } else {
                        RawResponse(header);
                    }
                } catch (SocketException e) {
                    LogUtils.LogClient(e.GetType()+":"+e.Message);
                }catch(ObjectDisposedException e) {
                    LogUtils.LogClient(e.GetType() + ":" + e.Message);
                }
            }
            LogUtils.LogClient("Client Finished");
        }
        ~Client() {
            Close();
        }
        public void Close(){
            try {
                isRunning = false;
                TcpClient.Close();
                LogUtils.LogClient("Client Shutdown");
            } catch (Exception e) {
                LogUtils.LogClient(e.Message);
            }
        }

        public static void RegisterHandler(Data.Types type, RequestHandler handler) {
            requestDispatcher[type] = handler;
        }

        public T Recv<T>() {
            return NetHelper.Recv<T>(Sock);
        }
        
        public void Response(params object[] responseBody) {
            response = header => RawResponse(header, responseBody);
            Event.Set();
        }

        private void RawResponse(RequestHeader header,params object[] responseBody) {
            Data.ResponseHeader responseHeader = new Data.ResponseHeader {
                Type = header.Type,
                BodyTypes = responseBody.Select(e => e.GetType().FullName).ToList()
            };
            NetHelper.Send(Sock, responseHeader);
            if (responseBody != null) {
                foreach (var e in responseBody)
                    NetHelper.SendProtoBuf(Sock, e);
            }
        }


        public override string ToString() {
            return Info.ToString();
        }

        /////////////////////////////////////////////

        static void InitHanlders() {
            RegisterHandler(Data.Types.GameStart, (c, r, b) => {
                c.Info = b[0] as ClientInfo;
                Log(c, r, "GameStart,UserIndex:" + c.Info.Index);
                c.Response();
            });
            RegisterHandler(Data.Types.PickRole, (c, r, b) => {
                var l = b[0] as Data.ListAdapter<int>;
                Log(c, r, String.Join(",", l.List));
                c.Response(new Data.TypeAdapter<int>(l.List.First()));
            });
            RegisterHandler(Data.Types.InitHandCards, (c, r, b) => {
                var l = b[0] as Data.ActionDesc;
                c.MyStatus.Cards.AddRange(l.Cards.List);
                Log(c, r, String.Join(",", l.Cards.List.Select(i => G.Cards[i])));
                Log(c, r, "HandCards:" + String.Join(",", c.MyStatus.Cards.List.Select(i => G.Cards[i])));
                c.Response();
            });
            RegisterHandler(Data.Types.ChangeStage, (c, r, b) => {
                var s = b[0] as StageChangeInfo;
                Log(c, r, "Turn:" + s.Turn + "  Stage:" + s.Stage);
                c.Response();
            });
            RegisterHandler(Data.Types.DrawCard, (c, r, b) => {
                var l = b[0] as Data.ActionDesc;
                if (l.User == c.Info.Index) {
                    LogUtils.Assert(l.Cards.List != null);
                    c.MyStatus.Cards.AddRange(l.Cards.List);
                    Log(c, r, String.Join(",", l.Cards.List.Select(i => G.Cards[i])));
                    Log(c, r, "HandCards:" + String.Join(",", c.MyStatus.Cards.List.Select(i => G.Cards[i])));
                } else {
                    LogUtils.Assert(l.Cards.List == null);
                    c.Status.UserStatus[l.User].Cards.Count += l.Cards.Count;
                    Log(c, r, "User(" + l.User + ") Drew "+l.Cards.Count+" Cards");
                    Log(c, r, "UserCardCount:" + String.Join(",",c.Status.UserStatus.Select(us => us.Cards.Count)));
                }                
                c.Response();
            });
            RegisterHandler(Data.Types.SyncStatus, (c, r, b) => {
                c.Status = b[0] as Data.Status;
                c.Response();
            });
            RegisterHandler(Data.Types.AskForAction, (c, r, b) => {
                foreach(var e in c.MyStatus.Cards.List) {
                    if (G.Cards[e].Face == CardFace.CF_JinJi) {
                        var user = (c.Status.Turn + 1) % c.Status.UserStatus.Length;
                        var ad = new ActionDesc {
                            ActionType = ActionType.AT_ATTACK,
                            User = c.Info.Index,
                            Users = new int[]{ user }.ToList(),
                            Card = e
                        };
                        c.Response(ad);
                        return;
                    }
                }
                c.Response(new ActionDesc {
                    ActionType = ActionType.AT_FINISH
                });
            });
            RegisterHandler(Types.DispAction, (c, r, b) => {
                var a = b[0] as ActionDesc;
                switch (a.ActionType) {
                case ActionType.AT_ATTACK:
                    Log(c, r, a.User + "使用" + CardInfo.ToString(a.Card) + "攻击" + a.Users[0]);
                    break;
                }
                c.Response();
            });
            RegisterHandler(Types.AskForCard, (c, r, b)=>{
                var a = b[0] as ActionDesc;
                if (a.User != c.Info.Index) {
                    c.Response();
                    return;
                }
                if (a.Cards.Count == 1) {
                    foreach(var i in c.MyStatus.Cards.List) {
                        if (G.Cards[i].Face == a.Cards[0]) {
                            c.Response(new ActionDesc {
                                ActionType = ActionType.AT_REPLY_CARD,
                                User = c.Info.Index,
                                Cards = new int[] { i}.ToList()
                            });
                            Log(c, r, "出牌：" + CardInfo.ToString(i));
                            return;
                        }
                    }
                    c.Response();
                    return;
                } else {
                    throw new NotImplementedException();
                }
            });
        }

        private static void Log(Client c,Data.RequestHeader r,String s) {
            LogUtils.LogClient(c.ToString() + r.ToString() + " : " + s);
        }
    }
}
