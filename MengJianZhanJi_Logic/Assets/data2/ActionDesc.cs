using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Assets.data {
    [ProtoContract]
    public enum ActionType {
        AT_CANCEL,
        AT_DRAW_CARD,
        AT_USE_CARD,
        AT_DROP_CARD,
        AT_REFUSE,
        AT_ALTER_HP,
        AT_ASK,
        AT_ASK_CARD,
        AT_ASK_DROP_CARD,
        AT_DEAD,
        AT_WIN,
    }

    [ProtoContract]
    public class ActionDesc {
        [ProtoMember(2)]
        public ActionType ActionType;
        [ProtoMember(3, IsRequired = true)]
        public int User = -1;
        [ProtoMember(4)]
        public List<int> Users;
        [ProtoMember(5, IsRequired = true)]
        public int Card = -1;
        [ProtoMember(6)]
        public PrivateList<int> Cards;
        [ProtoMember(7, IsRequired = true)]
        public int Skill = -1;
        [ProtoMember(8, IsRequired = true)]
        public int Arg1;
        [ProtoMember(9, IsRequired = true)]
        public int Arg2;
        [ProtoMember(10, IsRequired = true)]
        public string Message;


        public ActionDesc(){

        }

        public ActionDesc(ActionType type){
            this.ActionType=type;
        }
        public override string ToString() {
            return ToString(i => "User" + i);
        }

        private Func<int, String> userNameGetter;
        public string ToString(Func<int,String> userNameGetter) {
            if (userNameGetter != null) this.userNameGetter = userNameGetter;
            switch (ActionType) {
            case ActionType.AT_DRAW_CARD: return UserToString() + " 摸牌：" + CardsToString();
            case ActionType.AT_USE_CARD: return UserToString() + (Users == null || Users.Count == 0 ? "" : " 对 " + UsersToString()) + " 使用 " + CardToString()+CardsToString();
            case ActionType.AT_DROP_CARD: return UserToString() + "弃牌：" + CardsToString();
            case ActionType.AT_CANCEL: return UsersToString() + " 放弃" + (Message == null ? "" : ": " + Message);
            case ActionType.AT_REFUSE: return "拒绝操作:" + UserToString() + " " + Message;
            case ActionType.AT_ALTER_HP: return UserToString() + (Arg1 == 0 ? "" : " Hp " + Arg1) + (Arg2 == 0 ? "" : "MaxHp" + Arg2);
            case ActionType.AT_ASK: return "请 " + UserToString() + " 出牌";
            case ActionType.AT_ASK_CARD: return "请 " + UserToString() + "出牌: " + String.Join(",",Cards.List.Select(i => CardFace.getName(i)));
            case ActionType.AT_DEAD: return UserToString() + " 死亡";
            case ActionType.AT_ASK_DROP_CARD: return "请" + UserToString() + " 弃牌";
            case ActionType.AT_WIN: return "获胜者: " + UsersToString();
            default: return "还没有写描述: " + ActionType;
            }
        }

        private string UserToString() {
            if (User == -1) return "";
            return userNameGetter(User);
        }
        private string CardToString() {
            if (Card == -1) return "";
            return CardInfo.ToString(Card);
        }

        private string UsersToString() {
            if (Users==null||Users.Count==0) return "";
            return String.Join(",",Users.Select(i=>userNameGetter(i)));
        }

        private string CardsToString() {
            if (Cards==null) return "";
            if (Cards.Hide) return Cards.Count + "张牌";
            return String.Join("",Cards.List.Select(i => CardInfo.ToString(i)));
        }
    }

}
