using Assets.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.GameLogic {
    using T = Assets.Net.Data.Types;
    using Data = Assets.Net.Data;

    public class MainLogic {

        public ClientHandler[] clients;
        private Server server;
        private Random ran = new Random();

        private Status status;

        public MainLogic(Server server) {
            this.server = server;
        }

        public void Start(ClientHandler[] clients) {
            this.clients = clients;

            server.Broadcast(clients, T.GameStart);

            status = new Status();
            for (int i = 0x01000001; i <= 0x01000100; ++i) status.Roles.AddLast(i);
            Wash(status.Roles);

            BatchRequest(client => {
                LinkedList<int> ss = new LinkedList<int>();
                for (int i = 0; i < 5; ++i) {
                    ss.AddLast(status.Roles.First());
                    status.Roles.RemoveFirst();
                }
                var c = new MessageContext(client, T.PickRole,new Data.IntList(ss));
                return c;
            });

        }


        private void Wash<T>(ICollection<T> c) {
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
