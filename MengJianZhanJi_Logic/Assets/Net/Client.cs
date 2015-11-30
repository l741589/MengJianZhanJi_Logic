using Assets.Data;
using Assets.GameLogic;
using Assets.Utility;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.Net {
    public class Client : NetHelper{
        static Client() {
            InitHanlders();
        }

        private bool isRunning = true;
        public Data.ClientInfo Info { get; private set; }
        public Status Status { get; private set; }
        public UserStatus MyStatus { get { return Status.UserStatus[Info.Index]; } }

        public delegate void RequestHandler(Client client,Data.RequestHeader header,object[] body);
        private static Dictionary<Data.Types, RequestHandler> requestDispatcher = new Dictionary<Data.Types, RequestHandler>();
        public delegate void RequestActionHandler(Client client, Data.RequestHeader header, ActionDesc a);
        private static Dictionary<Data.ActionType, RequestActionHandler> requestActionDispatcher = new Dictionary<Data.ActionType, RequestActionHandler>();
        private AutoResetEvent Event = new AutoResetEvent(true);
        private Action<Data.RequestHeader> response;


        public Client(String ip, Data.ClientInfo info)
            : base(new TcpClient()) {
            Info = info;
            Client.Connect(ip, NetUtils.Port);
        }

        public void Loop() {
            RecvLooper.Post(() => {
                try {
                    Send(Info);
                    isRunning = true;
                    while (isRunning) {
                        try {
                            Data.RequestHeader header = Recv<Data.RequestHeader>();
                            LogUtils.LogClient(header.ToString());
                            object[] requestBody;
                            var types = header.BodyTypes;
                            if (types != null) {
                                int count = types.Count();
                                requestBody = new object[count];
                                for (int i = 0; i < count; ++i) {
                                    Type type = Type.GetType(types[i]);
                                    requestBody[i] = Recv(type);
                                }
                            } else {
                                requestBody = new object[0];
                            }
                            RequestHandler a;
                            if (requestDispatcher.TryGetValue(header.Type, out a)) {
                                a(this, header, requestBody);
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
                            LogUtils.LogClient(e.GetType() + ":" + e.Message);
                        } catch (ObjectDisposedException e) {
                            LogUtils.LogClient(e.GetType() + ":" + e.Message);
                        }
                    }
                    LogUtils.LogClient("Client Finished");
                } catch (Exception e) {
                    LogUtils.LogClient("Client Crashed:" + e);
                }
            });
            
        }
        ~Client() {
            Close();
        }


        public override void Close(){
            try {
                isRunning = false;
                base.Close();
                response = null;
                Event.Set();
                LogUtils.LogClient("Client Shutdown");
            } catch (Exception e) {
                LogUtils.LogClient(e.Message);
            }
        }

        public static void RegisterHandler(Data.Types type, RequestHandler handler) {
            requestDispatcher[type] = handler;
        }

        public static void RegisterHandler(ActionType type, RequestActionHandler handler) {
            requestActionDispatcher[type] = handler;
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
            Send(responseHeader);
            if (responseBody != null) {
                foreach (var e in responseBody)
                    Send(e);
            }
        }


        public override string ToString() {
            return Info.ToString();
        }

        /////////////////////////////////////////////

        static void InitHanlders() {
            RegisterHandler(Types.Action, (c, r, b) => {
                RequestActionHandler h;
                if (b == null || b.Length == 0) {
                    c.Response();
                    return;
                }
                var a = b[0] as ActionDesc;
                if (a == null) {
                    c.Response();
                    return;
                }
                Log(c, r, a.ToString());
                if (requestActionDispatcher.TryGetValue(a.ActionType, out h)) {
                    h(c, r, a);
                } else {
                    c.Response();
                }
            });
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
            RegisterHandler(Data.Types.ChangeStage, (c, r, b) => {
                var s = b[0] as StageChangeInfo;
                Log(c, r, "Turn:" + s.Turn + "  Stage:" + s.Stage);
                c.Response();
            });
            RegisterHandler(ActionType.AT_DRAW_CARD, (c, r, l) => {
                LogUtils.Assert(l.ActionType == ActionType.AT_DRAW_CARD);
                if (l.User == c.Info.Index) {
                    LogUtils.Assert(l.Cards.List != null);
                    c.MyStatus.Cards.AddRange(l.Cards.List);
                    Log(c, r, "HandCards:" + String.Join(",", c.MyStatus.Cards.List.Select(i => G.Cards[i])));
                } else {
                    LogUtils.Assert(l.Cards.List == null);
                    c.Status.UserStatus[l.User].Cards.Count += l.Cards.Count;
                    Log(c, r, "UserCardCount:" + String.Join(",",c.Status.UserStatus.Select(us => us.Cards.Count)));
                }                
                c.Response();
            });
            RegisterHandler(Data.Types.SyncStatus, (c, r, b) => {
                c.Status = b[0] as Data.Status;
                c.Response();
            });
            RegisterHandler(ActionType.AT_ASK, (c, r, a) => {
                if (a.Arg1 == 0) {
                    foreach (var e in c.MyStatus.Cards.List) {
                        if (G.Cards[e].Face == CardFace.CF_XiuLi&&c.MyStatus.Hp<c.MyStatus.MaxHp) {
                            c.Response(new ActionDesc {
                                ActionType = ActionType.AT_USE_CARD,
                                User = c.Info.Index,
                                Card = e
                            });
                            return;
                        }
                        if (G.Cards[e].Face == CardFace.CF_JinJi) {
                            var user = c.Status.Turn;
                            while (user == c.Status.Turn || c.Status.UserStatus[user].IsDead)
                                user = (user + 1) % c.Status.UserStatus.Length;
                            var ad = new ActionDesc {
                                ActionType = ActionType.AT_USE_CARD,
                                User = c.Info.Index,
                                Users = new int[] { user }.ToList(),
                                Card = e
                            };
                            c.Response(ad);
                            return;
                        }
                    }
                }
                c.Response(new ActionDesc {
                    ActionType = ActionType.AT_CANCEL
                });
            });
            RegisterHandler(ActionType.AT_ASK_CARD, (c, r, a)=>{
                if (a.User != c.Info.Index) {
                    c.Response();
                    return;
                }
                if (a.Cards.Count == 1) {
                    foreach(var i in c.MyStatus.Cards.List) {
                        if (G.Cards[i].Face == a.Cards[0]) {
                            c.Response(new ActionDesc {
                                ActionType = ActionType.AT_USE_CARD,
                                User = c.Info.Index,
                                Cards = new int[] { i}.ToList()
                            });
                            return;
                        }
                    }
                    c.Response(new ActionDesc {
                        ActionType = ActionType.AT_USE_CARD,
                        User=c.Info.Index,
                    });
                    return;
                } else {
                    throw new NotImplementedException();
                }
            });
            RegisterHandler(ActionType.AT_REFUSE, (c, r, b) => {
                c.Response();
            });
            RegisterHandler(ActionType.AT_ALTER_HP, (c, r, a) => {
                var user = c.Status.UserStatus[a.User];
                user.Hp += a.Arg1;
                user.MaxHp += a.Arg2;
                Log(c, r, String.Join(",", c.Status.UserStatus.Select(u => u.IsDead ? "DEAD" : u.Hp + "/" + u.MaxHp)));
                c.Response();
            });
            RegisterHandler(ActionType.AT_ASK_DROP_CARD, (c, r, a) => {
                c.Response(new ActionDesc(ActionType.AT_DROP_CARD) {
                    User = c.Info.Index,
                    Cards = c.MyStatus.Cards.List.Skip(c.MyStatus.Hp).ToList()
                });
            });
            RegisterHandler(ActionType.AT_DROP_CARD, (c, r, a) => {
                if (c.Info.Index == a.User) {
                    foreach (var i in a.Cards.List) c.MyStatus.Cards.Remove(i);
                    Log(c, r, "HandCards:" + String.Join(",", c.MyStatus.Cards.List.Select(i => G.Cards[i])));
                } else {
                    c.Status.UserStatus[a.User].Cards.Count -= a.Cards.Count;
                }
                c.Response();
            });
            RegisterHandler(ActionType.AT_WIN, (c, r, a) => {
                Log(c, r, a.Users.Contains(c.Info.Index) ? "胜利" : "失败");
                c.Response();
            });
        }

        private static void Log(Client c,Data.RequestHeader r,String s) {
            LogUtils.LogClient(c.ToString() + r.ToString() + " : " + s);
        }
    }
}
