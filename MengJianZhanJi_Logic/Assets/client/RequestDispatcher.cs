using Assets.data;
using Assets.utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.client {
    public class RequestDispatcher {
        private static Random Random = new Random();
        public delegate void RequestHandler(MessageContext c);
        public delegate void RequestActionHandler(MessageContext c, ActionDesc a);

        private static Dictionary<data.Types, RequestHandler> requestDispatcher = new Dictionary<data.Types, RequestHandler>();
        private static Dictionary<data.ActionType, RequestActionHandler> requestActionDispatcher = new Dictionary<data.ActionType, RequestActionHandler>();

        public static void Dispatch(MessageContext c) {
            RequestHandler a;
            if (requestDispatcher.TryGetValue(c.RequestHeader.Type, out a)) {
                a(c);
            } else {
                c.Response();
            }
        }

        public static void RegisterHandler(data.Types type, RequestHandler handler) {
            requestDispatcher[type] = handler;
        }

        public static void RegisterHandler(ActionType type, RequestActionHandler handler) {
            requestActionDispatcher[type] = handler;
        }

        public static void Log(MessageContext c, String s) {
            LogUtils.LogClient(c.Client.ToString() + c.RequestHeader.ToString() + " : " + s);
        }

        public static void EnableDispatchAction() {
            RegisterHandler(Types.Action, c => {
                RequestActionHandler h;
                var a = c.GetReq<ActionDesc>(0);
                if (a == null) {
                    c.Response();
                    return;
                }
                Log(c, a.ToString());
                if (requestActionDispatcher.TryGetValue(a.ActionType, out h)) {
                    h(c, a);
                } else {
                    c.Response();
                }
            });
        }

        public static void Mock() {
            
           
            /*RegisterHandler(data.Types.PickRole, c => {
                var l = c.GetReq<ListAdapter<int>>();
                Log(c, String.Join(",", l.List));
                c.Response(new data.TypeAdapter<int>(l.List.First()));
            });*/

            RegisterHandler(ActionType.AT_ASK_PICK_CARD, (c,a) => {
                if (a.User != c.MyStatus.Index) {
                    c.Response();
                } else {
                    a.ActionType = ActionType.AT_PICK_CARD;
                    a.Card = a.Cards[0];
                    c.Response(a);
                }
            });
            RegisterHandler(data.Types.ChangeStage, c => {
                var s = c.GetReq<StageChangeInfo>();
                Log(c, "Turn:" + s.Turn + "  Stage:" + s.Stage);
                c.Response();
            });
            RegisterHandler(ActionType.AT_DRAW_CARD, (c, l) => {
                LogUtils.Assert(l.ActionType == ActionType.AT_DRAW_CARD);
                var cl = c.Client;
                if (l.User == cl.Info.Index) {
                    LogUtils.Assert(l.Cards.List != null);
                    cl.MyStatus.Cards.AddRange(l.Cards.List);
                    Log(c, "HandCards:" + String.Join(",", cl.MyStatus.Cards.List.Select(i => G.Cards[i])));
                } else {
                    LogUtils.Assert(l.Cards.List == null);
                    cl.Status.UserStatus[l.User].Cards.Count += l.Cards.Count;
                    Log(c, "UserCardCount:" + String.Join(",", cl.Status.UserStatus.Select(us => us.Cards.Count)));
                }
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
                        case CardFace.CF_JinJi: {
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
                        case CardFace.CF_BanBenGengXin:
                        case CardFace.CF_UGuoHouQin: {
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
            RegisterHandler(ActionType.AT_ASK_CARD, (c, a) => {
                var cl = c.Client;
                if (a.User != cl.Info.Index) {
                    c.Response();
                    return;
                }
                if (a.IsSingleCard) {
                    foreach (var i in cl.MyStatus.Cards.List) {
                        if (G.Cards[i].Face == a.SingleCard) {
                            c.Response(new ActionDesc {
                                ActionType = ActionType.AT_USE_CARD,
                                User = cl.Info.Index,
                                Cards = new int[] { i }.ToList()
                            });
                            return;
                        }
                    }
                    c.Response(new ActionDesc {
                        ActionType = ActionType.AT_CANCEL,
                        User = cl.Info.Index,
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
            RegisterHandler(ActionType.AT_ASK_RELATION, (c, a) => {
                if (a.User != c.Info.Index) {
                    c.Response();
                    return;
                }
                if (c.MyStatus.Group == 0) {
                    if (c.Status.UserStatus.Any(u => u.Group == 1) && c.Status.UserStatus.Any(u => u.Group == 2)) {
                        c.Response(new ActionDesc(ActionType.AT_JOIN_GROUP) { User = c.Info.Index, Group = Random.Next() % 2 + 1 });
                    } else {
                        c.Response(new ActionDesc(ActionType.AT_SETUP_GROUP) { User = c.Info.Index });
                    }
                } else if (c.MyStatus.FlagShip) {
                    if (c.Status.UserStatus.Any(u => u != c.MyStatus && u.Group == c.MyStatus.Group)) {
                        c.Response(new ActionDesc(ActionType.AT_FIRE_MEMBER) {
                            User = c.Info.Index,
                            Users = new int[] { c.Status.UserStatus.First(u => u != c.MyStatus && u.Group == c.MyStatus.Group).Index }.ToList()
                        });
                    } else if (c.Status.UserStatus.Any(u => u.Group == 0)) {
                        c.Response(new ActionDesc(ActionType.AT_INVITE_MEMBER) {
                            User = c.Info.Index,
                            Users = new int[] { c.Status.UserStatus.First(u => u.Group == 0).Index}.ToList()
                        });
                    } else {
                        c.Response(new ActionDesc(ActionType.AT_CANCEL));
                    }
                } else {
                    c.Response(new ActionDesc(ActionType.AT_CANCEL));
                }                
            });
            RegisterHandler(ActionType.AT_ASK_VOTE, (c, a) => {
                c.Response(new ActionDesc(ActionType.AT_VOTE) { Arg1 = Random.Next() % 2 + 1 });
            });
        }
    }
}
