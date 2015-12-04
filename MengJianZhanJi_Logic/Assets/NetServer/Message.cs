using Assets.Net;
using Assets.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Assets.NetServer {

    public delegate void ResponseHandler(MessageContext context);

    public class MessageContext : ICloneable {
        public ClientHandler Client;
        public Data.Types Type;
        public Data.RequestHeader Request;
        public object[] RequestBody;
        public Data.ResponseHeader Response;
        public object[] ResponseBody;
        public ResponseHandler Handler;

        public MessageContext() {

        }

        public MessageContext(ClientHandler client, Data.ActionDesc a) {
            this.Client = client;
            this.Type = Data.Types.Action;
            this.RequestBody =new object[]{ a};
        }

        public MessageContext(ClientHandler client, Data.Types type, params object[] requestBody) {
            this.Client = client;
            this.Type = type;
            this.RequestBody = requestBody;
        }

        public T getResponse<T>(int index) where T :class{
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
            Request = new Data.RequestHeader() {
                Type = Type,
                BodyTypes = RequestBody.Select(i => i.GetType().FullName).ToList()
            };
            Client.Send(Request);
            foreach (var obj in RequestBody) {
                Client.Send(obj);
            }
        }

        public void RawRecv() {
            Response = Client.Recv<Data.ResponseHeader>();
            var types = Response.BodyTypes;
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
        }

        public void Recv() {
            do {
                RawRecv();
            } while (Request.Sid > Response.Sid);
            LogUtils.Assert(Response.Sid == Request.Sid);
        }

        public bool TryRecv() {
            if (Client.Client.Available <= 0) return false;
            RawRecv();
            if (Request.Sid > Response.Sid) return TryRecv();
            LogUtils.Assert(Request.Sid == Response.Sid);
            return true;
        }

        public void Handle() {
            LogUtils.LogServer(Client + Response.ToString());
            if (Handler != null) Handler(this);
        }

        public bool IsRecved {
            get {
                if (Request == null) return false;
                return Response != null && Response.Sid == Request.Sid;
            }
        }

        public void Trucate() {
            Type=Data.Types.TruncateMessage;
            Send();
        }
    }

    

    public class ClientHandler : NetHelper{

        public Data.ClientInfo ClientInfo { get; set; }
        public int Index { get { return ClientInfo.Index; } }

        public ClientHandler(Server server, TcpClient client) 
            : base(client, server.SendLooper, server.RecvLooper) {
            this.ClientInfo = Recv<Data.ClientInfo>();
            Client.ReceiveTimeout = 30000;
            Client.SendTimeout = 5000;
            LogUtils.LogServer(this.ClientInfo.Name + " joined");
        }

        public override string ToString() {
            return ClientInfo.ToString();
        }
    }
}
