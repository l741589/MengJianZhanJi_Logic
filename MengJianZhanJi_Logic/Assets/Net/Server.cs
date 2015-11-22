using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.Net {


    class Server {
        private TcpListener listener;
        private List<ClientHandler> clients=new List<ClientHandler>();
        private Action onServerStarted;
        private int state=0;        

        public Server(Action onStarted) {
            this.onServerStarted = onStarted;
            Loom.RunAsync(() => {
                try {
                    Work();
                } catch (Exception e) {
                    Debug.LogError("Server Crashed:" + e);
                    listener = null;
                }
            });          
        }

        ~Server() {
            Close();
        }

        

        public void Close(){
            state = -1;
            foreach (var e in clients) e.Sock.Close();
            if (listener != null) {
                listener.Server.Close();
                listener.Stop();
            }
            Debug.Log("Server Shutdown");
        }

        private void Work() {
            state = 0;
            listener = new TcpListener(IPAddress.Any, NetHelper.Port);
            listener.Server.SendTimeout=listener.Server.ReceiveTimeout = 5000;
            listener.Start();
            
            Debug.Log("Server Started");
            if (onServerStarted!=null) Loom.QueueOnMainThread(onServerStarted);
            while (state==0) {
                try { 
                    TcpClient client = listener.AcceptTcpClient();
                    clients.Add(new ClientHandler(client));
                } catch (SocketException e) {
                    Debug.LogError("Listen:"+e.Message);
                }
            }
            Debug.Log("Server WorkThread Finish");
        }


        class ClientHandler {
            
            public Data.ClientInfo ClientInfo { get; set; }
            public TcpClient Client { get; private set; }
            public Socket Sock { get { return Client.Client; } }
            public ClientHandler(TcpClient client) {
                this.Client = client;
                this.ClientInfo = NetHelper.Recv<Data.ClientInfo>(Sock);
                Client.ReceiveTimeout = 30000;
                Client.SendTimeout = 5000;
                Debug.Log(this.ClientInfo.Name + " joined");
            }

            public Data.ResponseHeader Request(String type, params object[] objs) {
                Data.ResponseHeader h = new Data.ResponseHeader() { Count = objs.Length, Type = type };
                NetHelper.Send(Sock, h);
                foreach (var obj in objs) NetHelper.Send(Sock,obj);
                return Recv<Data.ResponseHeader>();                
            }

            public T Recv<T>() {
                return NetHelper.Recv<T>(Sock);
            }
        }
    }
}
