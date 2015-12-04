using Assets.Data;
using Assets.Net;
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

namespace Assets.NetClient {
    public class Client : NetHelper{
        static Client() {
            InitHanlders();
        }

        private bool isRunning = true;
        public Data.ClientInfo Info { get; private set; }
        public Status Status { get; private set; }
        public UserStatus MyStatus { get { return Status.UserStatus[Info.Index]; } }

        public delegate void RequestHandler(MessageContext c);
        private static Dictionary<Data.Types, RequestHandler> requestDispatcher = new Dictionary<Data.Types, RequestHandler>();
        public delegate void RequestActionHandler(MessageContext c, ActionDesc a);
        private static Dictionary<Data.ActionType, RequestActionHandler> requestActionDispatcher = new Dictionary<Data.ActionType, RequestActionHandler>();


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
                            MessageContext c = MessageContext.NewRecv(this);
                            RequestHandler a;
                            if (requestDispatcher.TryGetValue(c.RequestHeader.Type, out a)) {
                                a(c);
                            } else {
                                c.Response();
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

        public override string ToString() {
            return Info.ToString();
        }

        /////////////////////////////////////////////

        static void InitHanlders() {
            RegisterHandler(Types.Action, c => {
                RequestActionHandler h;
                var a = c.ReqBody<ActionDesc>(0);
                if (a == null) {
                    c.Response();
                    return;
                }
                Log(c, a.ToString());
                if (requestActionDispatcher.TryGetValue(a.ActionType, out h)) {
                    h(c,a);
                } else {
                    c.Response();
                }
            });
            RegisterHandler(Data.Types.GameStart, c => {
                c.Client.Info = c.ReqBody<ClientInfo>(0);
                Log(c, "GameStart,UserIndex:" + c.Client.Info.Index);
                c.Response();
            });
            RegisterHandler(Data.Types.PickRole, c => {
                var l = c.ReqBody<ListAdapter<int>>();
                Log(c, String.Join(",", l.List));
                c.Response(new Data.TypeAdapter<int>(l.List.First()));
            });
            RegisterHandler(Data.Types.ChangeStage, c => {
                var s = c.ReqBody<StageChangeInfo>();
                Log(c, "Turn:" + s.Turn + "  Stage:" + s.Stage);
                c.Response();
            });
            RegisterHandler(ActionType.AT_DRAW_CARD, (c,l) => {
                LogUtils.Assert(l.ActionType == ActionType.AT_DRAW_CARD);
                var cl = c.Client;
                if (l.User == cl.Info.Index) {
                    LogUtils.Assert(l.Cards.List != null);
                    cl.MyStatus.Cards.AddRange(l.Cards.List);
                    Log(c, "HandCards:" + String.Join(",", cl.MyStatus.Cards.List.Select(i => G.Cards[i])));
                } else {
                    LogUtils.Assert(l.Cards.List == null);
                    cl.Status.UserStatus[l.User].Cards.Count += l.Cards.Count;
                    Log(c, "UserCardCount:" + String.Join(",",cl.Status.UserStatus.Select(us => us.Cards.Count)));
                }                
                c.Response();
            });
            RegisterHandler(Data.Types.SyncStatus, c => {
                var status = c.ReqBody<Data.Status>();
                if (status!=null) c.Client.Status = status;
                c.Response();
            });
            RegisterHandler(ActionType.AT_ASK, (c, a) => {
                var cl = c.Client;
                if (a.Arg1 == 0) {
                    foreach (var e in cl.MyStatus.Cards.List) {
                        switch (G.Cards[e].Face) {
                        case CardFace.CF_XiuLi:
                            if (cl.MyStatus.Hp < cl.MyStatus.MaxHp) {
                                c.Response(new ActionDesc {
                                    ActionType = ActionType.AT_USE_CARD,
                                    User = cl.Info.Index,
                                    Card = e
                                });
                                return;
                            }
                            break;
                        case CardFace.CF_JinJi:
                            {
                                var user = cl.Status.Turn;
                                while (user == cl.Status.Turn || cl.Status.UserStatus[user].IsDead)
                                    user = (user + 1) % cl.Status.UserStatus.Length;
                                var ad = new ActionDesc {
                                    ActionType = ActionType.AT_USE_CARD,
                                    User = cl.Info.Index,
                                    Users = new int[] { user }.ToList(),
                                    Card = e
                                };
                                c.Response(ad);
                                return;
                            }
                        case CardFace.CF_UGuoHouQin:
                            {
                                var ad = new ActionDesc {
                                    ActionType = ActionType.AT_USE_CARD,
                                    User = c.Info.Index,
                                    Card = e
                                };
                                c.Response(ad);
                                return;
                            }
                        }
                    }
                }
                c.Response(new ActionDesc {
                    ActionType = ActionType.AT_CANCEL
                });
            });
            RegisterHandler(ActionType.AT_ASK_CARD, (c, a)=>{
                var cl = c.Client;
                if (a.User != cl.Info.Index) {
                    c.Response();
                    return;
                }
                if (a.Cards.Count == 1) {
                    foreach(var i in cl.MyStatus.Cards.List) {
                        if (G.Cards[i].Face == a.Cards[0]) {
                            c.Response(new ActionDesc {
                                ActionType = ActionType.AT_USE_CARD,
                                User = cl.Info.Index,
                                Cards = new int[] { i}.ToList()
                            });
                            return;
                        }
                    }
                    c.Response(new ActionDesc {
                        ActionType = ActionType.AT_USE_CARD,
                        User=cl.Info.Index,
                    });
                    return;
                } else {
                    throw new NotImplementedException();
                }
            });
            RegisterHandler(ActionType.AT_REFUSE, (c, b) => {
                c.Response();
            });
            RegisterHandler(ActionType.AT_ALTER_HP, (c, a) => {
                var user = c.Client.Status.UserStatus[a.User];
                user.Hp += a.Arg1;
                user.MaxHp += a.Arg2;
                Log(c, String.Join(",", c.Client.Status.UserStatus.Select(u => u.IsDead ? "DEAD" : u.Hp + "/" + u.MaxHp)));
                c.Response();
            });
            RegisterHandler(ActionType.AT_ASK_DROP_CARD, (c, a) => {
                var cl = c.Client;
                c.Response(new ActionDesc(ActionType.AT_DROP_CARD) {                    
                    User = cl.Info.Index,
                    Cards = cl.MyStatus.Cards.List.Skip(cl.MyStatus.Hp).ToList()
                });
            });
            RegisterHandler(ActionType.AT_DROP_CARD, (c, a) => {
                var cl = c.Client;
                if (cl.Info.Index == a.User) {
                    foreach (var i in a.Cards.List) cl.MyStatus.Cards.Remove(i);
                    Log(c, "HandCards:" + String.Join(",", cl.MyStatus.Cards.List.Select(i => G.Cards[i])));
                } else {
                    cl.Status.UserStatus[a.User].Cards.Count -= a.Cards.Count;
                }
                c.Response();
            });
            RegisterHandler(ActionType.AT_WIN, (c, a) => {
                Log(c, a.Users.Contains(c.Client.Info.Index) ? "胜利" : "失败");
                c.Response();
            });
        }

        private static void Log(MessageContext c,String s) {
            LogUtils.LogClient(c.Client.ToString() + c.RequestHeader.ToString() + " : " + s);
        }
    }
}
