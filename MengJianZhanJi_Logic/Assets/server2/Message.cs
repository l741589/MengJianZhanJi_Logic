using Assets.net;
using Assets.utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Assets.server {

    public delegate void ResponseHandler(MessageContext context);

    public class MessageCache {
        public const int CacheSize = 20;
        public class Item{
            public data.ResponseHeader Header;
            public object[] Body;
        }

        private Dictionary<long, Item> Map = new Dictionary<long, Item>();
        private LinkedList<Item> Queue = new LinkedList<Item>();

        public void AddCache(MessageContext c) {
            var item = new Item { Header = c.ResponseHeader, Body = c.ResponseBody };
            Queue.AddLast(item);
            Map.Add(item.Header.Sid, item);
            while (Queue.Count > CacheSize) {
                var i = Queue.First.Value;
                Queue.RemoveFirst();
                Map.Remove(i.Header.Sid);
            }
        }

        public bool ObtainCache(MessageContext c) {
            var sid=c.ResponseHeader.Sid;
            if (!Map.ContainsKey(sid)) return false;
            var item = Map[sid];
            c.ResponseHeader = item.Header;
            c.ResponseBody = item.Body;
            return true;
        }
    }

    public class MessageContext : ICloneable {
        public ClientHandler Client;
        public data.Types Type;
        public data.RequestHeader RequestHeader;
        public object[] RequestBody;
        public data.ResponseHeader ResponseHeader;
        public object[] ResponseBody;
        public ResponseHandler Handler;
        private static MessageCache Cache = new MessageCache();
        public MessageContext() {

        }

        public MessageContext(ClientHandler client, data.ActionDesc a) {
            this.Client = client;
            this.Type = data.Types.Action;
            this.RequestBody =new object[]{ a};
        }

        public MessageContext(ClientHandler client, data.Types type, params object[] requestBody) {
            this.Client = client;
            this.Type = type;
            this.RequestBody = requestBody;
        }

        public T GetRes<T>(int index = 0) where T :class{
            if (index >= ResponseBody.Length) return default(T);
            return ResponseBody[index] as T;
        }

        public object Clone() {
            return MemberwiseClone();
        }

        public override string ToString() {
            return Client.ToString() + Type +
                (RequestBody != null ? String.Join(",", RequestBody) : "NoReq") + " " +
                (ResponseBody != null ? String.Join(",", ResponseBody) : "NoReq");
        }

        public void Send() {
            RequestHeader = new data.RequestHeader() {
                Type = Type,
                BodyTypes = RequestBody.Select(i => i.GetType().FullName).ToList()
            };
            Client.Send(RequestHeader);
            foreach (var obj in RequestBody) {
                Client.Send(obj);
            }
        }

        public void RawRecv() {
            ResponseHeader = Client.Recv<data.ResponseHeader>();
            if (Cache.ObtainCache(this)) return;
            var types = ResponseHeader.BodyTypes;
            if (types != null) {
                int count = types.Count();
                ResponseBody = new object[count];
                for (int i = 0; i < count; ++i) {
                    Type type = System.Type.GetType(types[i]);
                    ResponseBody[i] = Client.Recv(type);
                }
            } else {
                ResponseBody = new object[0];
            }
            Cache.AddCache(this);
        }

        public void Recv() {
            do {
                RawRecv();
            } while (RequestHeader.Sid > ResponseHeader.Sid);
            LogUtils.Assert(ResponseHeader.Sid == RequestHeader.Sid);
        }

        public bool TryRecv() {
            if (Client.Client.Available <= 0) return false;
            RawRecv();
            if (RequestHeader.Sid > ResponseHeader.Sid) return TryRecv();
            LogUtils.Assert(RequestHeader.Sid == ResponseHeader.Sid);
            return true;
        }

        public void Handle() {
            LogUtils.LogServer(Client + ResponseHeader.ToString());
            if (Handler != null) Handler(this);
        }

        public bool IsRecved {
            get {
                if (RequestHeader == null) return false;
                return ResponseHeader != null && ResponseHeader.Sid == RequestHeader.Sid;
            }
        }

        public void Trucate() {
            Type=data.Types.TruncateMessage;
            Send();
        }
    }

    

    public class ClientHandler : NetHelper{

        public data.ClientInfo ClientInfo { get; set; }
        public int Index { get { return ClientInfo.Index; } }

        public ClientHandler(Server server, TcpClient client) 
            : base(client, server.SendLooper, server.RecvLooper) {
            this.ClientInfo = Recv<data.ClientInfo>();
            Client.ReceiveTimeout = 30000;
            Client.SendTimeout = 5000;
            LogUtils.LogServer(this.ClientInfo.Name + " joined");
        }

        public override string ToString() {
            return ClientInfo.ToString();
        }
    }
}
