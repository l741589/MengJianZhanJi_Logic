using Assets.Util;
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
        public String type;
        public Data.RequestHeader request;
        public object[] requestBody;
        public Data.ResponseHeader response;
        public object[] responseBody;
        public ResponseHandler handler;

        public MessageContext() {

        }

        public MessageContext(ClientHandler client, String type, params object[] requestBody) {
            this.client = client;
            this.type = type;
            this.requestBody = requestBody;
        }

        public object Clone() {
            return MemberwiseClone();
        }
    }

    public class ClientHandler {

        public Data.ClientInfo ClientInfo { get; set; }
        public TcpClient Client { get; private set; }
        public Socket Sock { get { return Client.Client; } }
        public int Index { get; set; }
        public ClientHandler(TcpClient client) {
            this.Client = client;
            this.ClientInfo = NetHelper.Recv<Data.ClientInfo>(Sock);
            Client.ReceiveTimeout = 30000;
            Client.SendTimeout = 5000;
            LogUtils.LogServer(this.ClientInfo.Name + " joined");
        }

        public T Recv<T>() {
            return NetHelper.Recv<T>(Sock);
        }

        public override string ToString() {
            return ClientInfo.ToString();
        }
    }
}
