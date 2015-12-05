using Assets.data;
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
        
        public UsingCardState() {
            
        }

        public UsingCardState(ActionDesc a) {
            Init(a);
        }

        public UsingCardState Init(ActionDesc a) {
            A = a;            
            return this;
        }

        public override State Run() {
            Id = A.Card;
            Self = Status.UserStatus[A.User];
            Card = G.Cards[Id];

            if (A.Users == null || A.Users.Count == 0) {
                Target = Self;
                Using();
            } else {
                foreach (var i in A.Users) {
                    Target = Status.UserStatus[i];
                    Using();
                }
            }
            EffectAfterAll();
            return null;
        }

        public virtual void Using(){
            if (!IsValid()) {
                Server.Request(CurrentClient, Types.Action, new ActionDesc {
                    ActionType = ActionType.AT_REFUSE,
                    Message = "非法的出牌: " + Card
                });
                return;
            }
            Self.Cards.Remove(Id);
            Broadcast(A);
            if (IsCanceled()) return;
            Effect();
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
            return ca == null || ca.Arg1 == 1;
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

    }

    class UseUGuoHouQin : UseStrategy{
        public override void Effect() {
            PrivateList<int> cards = DrawCard(Target, 2).ToList();
            BatchRequest(cx => new server.MessageContext(cx, new ActionDesc {
                ActionType = ActionType.AT_DRAW_CARD,
                User = Target.Index,
                Cards = cards.Clone(cx.ClientInfo.Index != Target.Index)
            }));
        }
    }
}
