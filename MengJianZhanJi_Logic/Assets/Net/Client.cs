using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Assets.Net {
    class Client {
        private TcpClient TcpClient { get; set; }
        private bool isRunning = true;
        public Socket Sock { get { return TcpClient.Client; } }
        private Data.ClientInfo Info { get; set; }
        public delegate object RequestHandler(Client client,Data.RequestHeader header);
        private static Dictionary<String, RequestHandler> requestDispatcher = new Dictionary<string, RequestHandler>();

        public Client(String ip,Data.ClientInfo info) {
            TcpClient = new TcpClient();
            Info=info;
            TcpClient.Connect(ip, NetHelper.Port);
        }
        public void Loop() {
            NetHelper.Send(Sock, Info);
            isRunning = true;
            while (isRunning) {
                try {
                    Data.RequestHeader header = NetHelper.Recv<Data.RequestHeader>(Sock);
                    Debug.Log(header.ToString());
                    RequestHandler a;
                    if (requestDispatcher.TryGetValue(header.Type, out a)) {
                        object responseBody=a(this, header);
                        Data.ResponseHeader responseHeader = new Data.ResponseHeader {
                            Type=header.Type,
                            Count=responseBody==null?0:1
                        };
                        if (responseBody != null) {
                            NetHelper.SendProtoBuf(Sock, responseBody);
                        }
                    }
                } catch (SocketException e) {
                    Debug.LogError(e.GetType()+":"+e.Message);
                }
            }
            Debug.Log("Client Finished");
        }
        ~Client() {
            Close();
        }
        public void Close(){
            try {
                isRunning = false;
                TcpClient.Close();
                Debug.Log("Client Shutdown");
            } catch (Exception e) {
                Debug.LogError(e.Message);
            }
        }

        public static void RegisterHandler(String type, RequestHandler handler) {
            requestDispatcher[type] = handler;
        }

        public T Recv<T>() {
            return NetHelper.Recv<T>(Sock);
        }

        /////////////////////////////////////////////

        static void InitHanlders() {
            
        }
    }
}
