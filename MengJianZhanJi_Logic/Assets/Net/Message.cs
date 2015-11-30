using Assets.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Assets.Net {

    public delegate void ResponseHandler(MessageContext context);

    public class MessageContext : ICloneable {
        public ClientHandler client;
        public Data.Types type;
        public Data.RequestHeader request;
        public object[] requestBody;
        public Data.ResponseHeader response;
        public object[] responseBody;
        public ResponseHandler handler;

        public MessageContext() {

        }

        public MessageContext(ClientHandler client, Data.ActionDesc a) {
            this.client = client;
            this.type = Data.Types.Action;
            this.requestBody =new object[]{ a};
        }

        public MessageContext(ClientHandler client, Data.Types type, params object[] requestBody) {
            this.client = client;
            this.type = type;
            this.requestBody = requestBody;
        }

        public T getResponse<T>(int index) where T :class{
            if (index >= responseBody.Length) return default(T);
            return responseBody[index] as T;
        }

        public object Clone() {
            return MemberwiseClone();
        }

        public override string ToString() {
            return client.ToString() + type +
                (requestBody != null ? String.Join(",", requestBody) : "NoReq") + " " +
                (responseBody != null ? String.Join(",", responseBody) : "NoReq");
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

      /*  public T Recv<T>() {
            return NetHelper.Recv<T>(Sock);
        }*/

        public override string ToString() {
            return ClientInfo.ToString();
        }
    }
}
