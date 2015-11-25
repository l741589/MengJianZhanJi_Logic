using Assets.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.Net {

    public class Server {
        private TcpListener listener;
        private List<ClientHandler> clients=new List<ClientHandler>();
        private Action onServerStarted;
        private int state=0;   
        public int State { get { return state; } }     

        public Server(Action onStarted) {
            this.onServerStarted = onStarted;
            Loom.RunAsync(() => {
                try {
                    Work();
                } catch (Exception e) {
                    LogUtils.LogServer("Server Crashed:" + e);
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
            LogUtils.LogServer("Server Shutdown");
        }

        private void Work() {
            state = 0;
            listener = new TcpListener(IPAddress.Any, NetHelper.Port);
            listener.Server.SendTimeout=listener.Server.ReceiveTimeout = 5000;
            listener.Start();

            LogUtils.LogServer("Server Started");
            if (onServerStarted!=null) Loom.QueueOnMainThread(onServerStarted);
            while (state==0) {
                try { 
                    TcpClient client = listener.AcceptTcpClient();
                    clients.Add(new ClientHandler(client));
                } catch (SocketException e) {
                    LogUtils.LogServer("Listen:"+e.Message);
                } catch (ObjectDisposedException e) {
                    LogUtils.LogClient(e.GetType() + ":" + e.Message);
                }
            }
            LogUtils.LogServer("Server WorkThread Finish");
        }


        public MessageContext Request(ClientHandler client, Data.Types type, params object[] objs) {
            var c = new MessageContext(client, type, objs);
            Request(c);
            return c;
        }

        public void Broadcast(ClientHandler[] clients,Data.Types type, params object[] objs) {
            int len = clients.Length;
            MessageContext[] cs = new MessageContext[len];
            for (int i = 0; i < len; ++i) cs[i] = new MessageContext(clients[i], type, objs);
            Request(cs);
        }

        public void Request(params MessageContext[] contexts) {
            foreach (var e in contexts) {
                e.request = new Data.RequestHeader() {
                    Type = e.type,
                    BodyTypes=e.requestBody.Select(i=>i.GetType().FullName).ToList()
                };
                NetHelper.Send(e.client.Sock, e.request);
                foreach (var obj in e.requestBody) {
                    NetHelper.Send(e.client.Sock, obj);
                }
            }
            foreach (var e in contexts) {
                e.response = NetHelper.Recv<Data.ResponseHeader>(e.client.Sock);
                var types = e.response.BodyTypes;
                if (types != null) {
                    int count = types.Count();
                    e.responseBody = new object[count];
                    for (int i = 0; i < count; ++i) {
                        Type type = Type.GetType(types[i]);
                        e.responseBody[i] = NetHelper.Recv(e.client.Sock, type);
                    }
                } else {
                    e.responseBody = new object[0];
                }
                LogUtils.LogServer(e.client+e.response.ToString());
                if (e.handler != null) e.handler(e);
            }
        }

        public void Start() {
            state = 1;
            Loom.RunAsync(() => {
                try {
                    GameLogic.MainLogic logic = new GameLogic.MainLogic(this);
                    logic.Start(clients.ToArray());
                }catch(SocketException e) {
                    LogUtils.LogServer(e.Message);
                } catch (ObjectDisposedException e) {
                    LogUtils.LogClient(e.GetType() + ":" + e.Message);
                }
            });
        }       
    }
}
