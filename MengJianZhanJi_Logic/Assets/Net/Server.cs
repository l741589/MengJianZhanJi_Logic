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


        public MessageContext Request(ClientHandler client,String type, params object[] objs) {
            var c = new MessageContext(client, type, objs);
            Request(c);
            return c;
        }

        public void Broadcast(ClientHandler[] clients,String type, params object[] objs) {
            int len = clients.Length;
            MessageContext[] cs = new MessageContext[len];
            for (int i = 0; i < len; ++i) cs[i] = new MessageContext(clients[i], type, objs);
            Request(cs);
        }

        public void Request(params MessageContext[] contexts) {
            foreach (var e in contexts) {
                e.request = new Data.RequestHeader() { Count = e.requestBody.Length, Type = e.type };
                NetHelper.Send(e.client.Sock, e.request);
                foreach (var obj in e.requestBody) NetHelper.Send(e.client.Sock, obj);
            }
            foreach (var e in contexts) {
                e.response = NetHelper.Recv<Data.ResponseHeader>(e.client.Sock);
                Debug.Log("("+e.client.ClientInfo.Name+')'+e.response.ToString());
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
                    Debug.LogError(e.Message);
                }
            });
        }       
    }
}
