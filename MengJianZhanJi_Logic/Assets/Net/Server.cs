﻿using Assets.Data;
using Assets.GameLogic;
using Assets.Utility;
using System;
using System.Collections.Generic;
using System.IO;
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

        public void RequestFirst(params MessageContext[] contexts) {
            foreach (var e in contexts) {
                e.request = new Data.RequestHeader() {
                    Type = e.type,
                    BodyTypes = e.requestBody.Select(i => i.GetType().FullName).ToList()
                };
                e.client.Send(e.request);
                foreach (var obj in e.requestBody) {
                    e.client.Send(obj);
                }
            }
            foreach (var e in contexts) {
                e.response = e.client.Recv<Data.ResponseHeader>();
                var types = e.response.BodyTypes;
                if (types != null) {
                    int count = types.Count();
                    e.responseBody = new object[count];
                    for (int i = 0; i < count; ++i) {
                        Type type = Type.GetType(types[i]);
                        e.responseBody[i] = e.client.Recv(type);
                    }
                } else {
                    e.responseBody = new object[0];
                }

            }
            foreach (var e in contexts) {
                LogUtils.LogServer(e.client + e.response.ToString());
                if (e.handler != null) e.handler(e);
            }
        }

        public void Request(params MessageContext[] contexts) {
            foreach (var e in contexts) {
                e.request = new Data.RequestHeader() {
                    Type = e.type,
                    BodyTypes=e.requestBody.Select(i=>i.GetType().FullName).ToList()
                };
                e.client.SendAsync(e.request);
                foreach (var obj in e.requestBody) {
                    e.client.SendAsync(obj);
                }
            }
            foreach (var e in contexts) {
                e.response = e.client.Recv<Data.ResponseHeader>();
                var types = e.response.BodyTypes;
                if (types != null) {
                    int count = types.Count();
                    e.responseBody = new object[count];
                    for (int i = 0; i < count; ++i) {
                        Type type = Type.GetType(types[i]);
                        e.responseBody[i] = e.client.Recv(type);
                    }
                } else {
                    e.responseBody = new object[0];
                }
            }
            foreach (var e in contexts) {
                LogUtils.LogServer(e.client+e.response.ToString());
                if (e.handler != null) e.handler(e);
            }
        }

        public void Start() {
            state = 1;
            Loom.RunAsync(() => {
                try {
                    StateEnvironment env = new StateEnvironment {
                        Clients = clients.ToArray(),
                        Random = new Random(),
                        Server = this,
                        Status = new Status()
                    };
                    StateMachine = new MainState(env);
                    StateMachine.Next();
                }catch(SocketException e) {
                    LogUtils.LogServer(e.Message);
                } catch (ObjectDisposedException e) {
                    LogUtils.LogServer(e.GetType() + ":" + e.Message);
                }catch(IOException e) {
                    LogUtils.LogServer(e.Message);
                }catch(ThreadInterruptedException e) {
                    LogUtils.LogServer(e.Message);
                }
                LogUtils.LogServer("Logic Thread Finished");
            }).Name="LogicThread";
        }       
    }
}
