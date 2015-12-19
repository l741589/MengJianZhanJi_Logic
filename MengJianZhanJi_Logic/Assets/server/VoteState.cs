using Assets.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.server {
    class VoteState : State{

        private IEnumerable<int> users;
        private String message;

        public VoteState(String msg, params int[] users)
            : this(users, msg) {

        }

        public VoteState(IEnumerable<int> users,String message) {
            this.users = users;
            this.message = message;
        }

        public override object Run() {
            ActionDesc ret = new ActionDesc(ActionType.AT_VOTE);
            ret.Arg1 = ret.Arg2 = 0;
            ret.Users = new List<int>();
            BatchRequest(c => new ActionDesc(ActionType.AT_ASK_VOTE) {
                User = users.Contains(c.Index) ? c.Index : -1,
                Users = users.ToList(),
                Message = message
            }, c => {
                if (!users.Contains(c.Client.Index)) return;
                ActionDesc d = c.GetRes<ActionDesc>();
                int flagVote = 0;
                switch (d.Arg1) {
                case 1: ++ret.Arg1; 
                    ret.Users.Add(c.Client.Index);
                    if (Status.UserStatus[c.Client.Index].FlagShip) flagVote = 1;
                    break;
                case 2: ++ret.Arg2;
                    if (Status.UserStatus[c.Client.Index].FlagShip) flagVote = 2;
                    break;
                }
                if (ret.Arg1 == ret.Arg2) {
                    if (flagVote == 1) ++ret.Arg1;
                    else ++ret.Arg2;
                }
                ret.Success = ret.Arg1 > ret.Arg2;
                Result = ret;
            });
            return null;
        }
    }
}
