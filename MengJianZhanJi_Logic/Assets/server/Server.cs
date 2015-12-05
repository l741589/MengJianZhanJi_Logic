using Assets.data;
using Assets.net;
using Assets.utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.server {

    public class Server {
        private TcpListener listener;
        private List<ClientHandler> clients=new List<ClientHandler>();
        private Action onServerStarted;
        private int state=0;   
        public int State { get { return state; } }
        public Looper RecvLooper { get; private set; }
        public Looper SendLooper { get; private set; }
        private State StateMachine;

        public Server(Action onStarted) {
            this.onServerStarted = onStarted;
            RecvLooper = new Looper();
            RecvLooper.StartNewThread("ServerRecvLooper");
            SendLooper = new Looper();
            SendLooper.StartNewThread("ServerSendLooper");
            Loom.RunAsync(() => {
                try {
                    Work();
                } catch (Exception e) {
                    LogUtils.LogServer("Server Crashed:" + e);
                    listener = null;
                }
            }).Name="Listener Thread";
        }

        ~Server() {
            Close();
        }

        public void Close(){
            state = -1;
            foreach (var e in clients) e.Close();
            if (listener != null) {
                listener.Server.Close();
                listener.Stop();
            }
            if (StateMachine != null) StateMachine.Close();
            LogUtils.LogServer("Server Shutdown");
        }

        private void Work() {
            state = 0;
            listener = new TcpListener(IPAddress.Any, NetUtils.Port);
            listener.Server.SendTimeout=listener.Server.ReceiveTimeout = 5000;
            listener.Start();

            LogUtils.LogServer("Server Started");
            if (onServerStarted!=null) Loom.QueueOnMainThread(onServerStarted);
            while (state==0) {
                try { 
                    TcpClient client = listener.AcceptTcpClient();
                    clients.Add(new ClientHandler(this,client));
                } catch (SocketException e) {
                    LogUtils.LogServer("Listen:"+e.Message);
                } catch (ObjectDisposedException e) {
                    LogUtils.LogClient(e.GetType() + ":" + e.Message);
                }
            }
            LogUtils.LogServer("Listener Thread Finish");
        }


        public MessageContext Request(ClientHandler client, data.Types type, params object[] objs) {
            var c = new MessageContext(client, type, objs);
            Request(c);
            return c;
        }

        public void Broadcast(ClientHandler[] clients,data.Types type, params object[] objs) {
            int len = clients.Length;
            MessageContext[] cs = new MessageContext[len];
            for (int i = 0; i < len; ++i) cs[i] = new MessageContext(clients[i], type, objs);
            Request(cs);
        }

        public void RequestOne(params MessageContext[] contexts) {
            foreach (var e in contexts) e.Send();
            while (true) {
                foreach (var e in contexts) 
                    if (e.TryRecv()) goto SUCCESS;
                Thread.Sleep(100);
            }
            SUCCESS:
            foreach (var e in contexts) {
                if (!e.IsRecved) 
                    e.Trucate();
                
            }
            foreach (var e in contexts) {
                if (e.IsRecved) {
                    e.Handle();
                } else {
                    e.Recv();
                }
            }
        }

        public void Request(params MessageContext[] contexts) {
            foreach (var e in contexts) e.Send();
            foreach (var e in contexts) e.Recv();
            foreach (var e in contexts) e.Handle();
        }

        public void Start() {
            state = 1;
            RecvLooper.Post(() => {
                try {
                    StateEnvironment env = new StateEnvironment {
                        Clients = clients.ToArray(),
                        Random = new Random(),
                        Server = this,
                        Status = new Status()
                    };
                    StateMachine = new MainState(env);
                    StateMachine.Next();
                } catch (SocketException e) {
                    LogUtils.LogServer(e.Message);
                } catch (ObjectDisposedException e) {
                    LogUtils.LogServer(e.GetType() + ":" + e.Message);
                } catch (IOException e) {
                    LogUtils.LogServer(e.Message);
                } catch (ThreadInterruptedException e) {
                    LogUtils.LogServer(e.Message);
                }
                LogUtils.LogServer("Logic Thread Finished");
            });
        }       
    }
}
