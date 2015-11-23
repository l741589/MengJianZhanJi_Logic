using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.Net {
    class Client {
        static Client() {
            InitHanlders();
        }

        private TcpClient TcpClient { get; set; }
        private bool isRunning = true;
        public Socket Sock { get { return TcpClient.Client; } }
        private Data.ClientInfo Info { get; set; }
        public delegate void RequestHandler(Client client,Data.RequestHeader header);
        private static Dictionary<String, RequestHandler> requestDispatcher = new Dictionary<string, RequestHandler>();
        private AutoResetEvent Event = new AutoResetEvent(true);
        private Action<Data.RequestHeader> response;
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
                        a(this, header);
                        Event.WaitOne();
                        if (!isRunning) break;
                        if (response != null) {
                            response(header);
                            response = null;
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
        public void Response() {
            Response<object>(null);
        }
        public void Response<T>(T responseBody) {
            response = header => {
                Data.ResponseHeader responseHeader = new Data.ResponseHeader {
                    Type = header.Type,
                    Count = responseBody == null ? 0 : 1
                };
                NetHelper.Send(Sock, responseHeader);
                if (responseBody != null) {
                    NetHelper.SendProtoBuf(Sock, responseBody);
                }
            };
            Event.Set();
        }

        /////////////////////////////////////////////

        static void InitHanlders() {
            RegisterHandler(Data.Types.GameStart, (c, r) => {
                c.Response();
            });
            RegisterHandler(Data.Types.PickRole, (c, r) => {
                var l=c.Recv<Data.IntList>();
                Debug.Log(String.Join(",", l.List));
                c.Response();
            });
        }
    }
}
