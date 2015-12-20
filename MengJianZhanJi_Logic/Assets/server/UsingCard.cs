using Assets.data;
using Assets.utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.server {
    partial class UsingCardState : State{
        protected int Id;
        protected CardInfo Card;
        protected UserStatus Self;
        protected UserStatus Target;
        protected ActionDesc A;
        protected State Next;
        
        public UsingCardState() {
            
        }

        public UsingCardState(ActionDesc a) {
            Init(a);
        }

        public UsingCardState Init(ActionDesc a) {
            A = a;
            LogUtils.Assert(A.IsSingleCard);
            return this;
        }

        public virtual ActionDesc PreRun(ActionDesc a) {
            return a;
        }

        public override object Run() {
            A = PreRun(A);
            Next = null;
            if (A == null) A = new ActionDesc(ActionType.AT_USE_CARD);
            Id = A.SingleCard;
            Self = A.User == -1 ? null : Status.UserStatus[A.User];
            Card = Id >= 0 ? G.Cards[Id] : null;
            bool result = true;
            if (A.Users == null || A.Users.Count == 0) {
                Target = Self;
                result &= Using();
            } else {
                foreach (var i in A.Users) {
                    Target = Status.UserStatus[i];
                    result &= Using();
                }
            }
            EffectAfterAll();
            Result = A.Clone();
            (Result as ActionDesc).Success = result;
            return Next;
        }

        public virtual bool Using(){
            if (!IsValid()) {
                Server.Request(CurrentClient, Types.Action, new ActionDesc {
                    ActionType = ActionType.AT_REFUSE,
                    Message = "非法的出牌: " + Card
                });
                return false;
            }
            if (Id>=0&&Self!=null&&Self.Cards != null) Self.Cards.Remove(Id);
            if (Id>=0) Broadcast(A);
            
            if (IsCanceled()) return false;
            Effect();
            return true;
        }
            
        public virtual bool IsValid(){
            return true;
        }

        public virtual bool IsCanceled() {
            return false;
        }

        public virtual void Effect(){

        }

        public virtual void EffectAfterAll() {

        }
    }

    partial class UsingCardState {
        private static Dictionary<int, Func<ActionDesc, UsingCardState>> map = new Dictionary<int, Func<ActionDesc,UsingCardState>>();
        public static UsingCardState Create(ActionDesc a) {
            var face = G.Cards[a.Card].Face;
            if (!map.ContainsKey(face)) return new UsingCardState().Init(a);
            return map[face](a);
        }

        public static void Register(int face, Func<ActionDesc, UsingCardState> creator) {
            map.Add(face, creator);
        }

        public static void Register<T>(int face) where T:UsingCardState,new() {
            map.Add(face, a => new T().Init(a));
        }

        static UsingCardState() {
            Register<UseJinJi>(CardFace.CF_JinJi);
            Register<UseXiuLi>(CardFace.CF_XiuLi);
            Register<UseUGuoHouQin>(CardFace.CF_UGuoHouQin);
            //Register<UseBanBenGengXin>(CardFace.CF_BanBenGengXin);
        }        
    }

    class UseJinJi : UsingCardState {

        private bool Attacked {
            get {
                var us = Parent as UseCardState;
                if (us == null) return false;
                return us.Attacked;
            }
            set {
                var us = Parent as UseCardState;
                if (us == null) return;
                us.Attacked = value;
            }
        }

        public override bool IsValid() {
            return !Attacked;
        }

        public override bool IsCanceled() {
            var ca = RunSub(new AskForCardState(Target.Index, new List<int> { CardFace.CF_HuiBi })) as ActionDesc;
            return ca == null || ca.Success;
        }

        public override void Effect() {
            RunSub(new AlterHpState(Target.Index, -1));
        }

        public override void EffectAfterAll() {
            Attacked = true;
        }
    }

    class UseXiuLi : UsingCardState {
        public override void Effect() {
            RunSub(new AlterHpState(Target.Index, 1));
        }
    }

    class UseStrategy : UsingCardState {
        public override bool IsCanceled() {
            var result = RunSub(new AskForCardState(Target.Index, CardFace.CF_ZhanShuYuHui)) as ActionDesc;
            //var result = RunSub(new AskForCardSyncAll(CardFace.CF_ZhanShuYuHui).Cancelable(s => IsCanceled())) as ActionDesc;
            return result==null||result.Success;
        }
    }

    class UseUGuoHouQin : UseStrategy{

        public override ActionDesc PreRun(ActionDesc a) {
            if (a.Group > 0) a.Users = GroupUsersIndex(a.Group);
            else a.Users = null;
            return a;
        }

        public override void Effect() {
            PrivateList<int> cards =null;
            if (A.Group == 0) {
                cards= DrawCard(Target, 2).ToList();
            } else {
                cards=DrawCard(Target, 1).ToList();
            }
            if (cards != null) {
                BatchRequest(cx => new server.MessageContext(cx, new ActionDesc {
                    ActionType = ActionType.AT_DRAW_CARD,
                    User = Target.Index,
                    Cards = cards.Clone(cx.ClientInfo.Index != Target.Index)
                }));
            }
        }
    }

    class UseBanBenGengXin : UsingCardState {

        class _UseBanBenGengXin : UseStrategy {

            public List<int> cards;
            public UserStatus user;

            public override void Effect() {
                ActionDesc a = RunSub(new PickCardState(user.Index, cards)) as ActionDesc;
                if (a == null || a.Card < 0) {
                    Next = this;
                    return;
                }
                Result = a;
                Status.UserStatus[user.Index].Cards.Add(a.Card);
            }
        }

        public override void Effect() {
            var cards=DrawCard(null, Status.AliveUserCount).ToList();
            CircleCall(u => {
                var a=RunSub(new _UseBanBenGengXin { cards = cards, user = u }.Init(A)) as ActionDesc;
                cards.Remove(a.Card);
                return false;
            });
        }
    }
}
