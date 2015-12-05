using Assets.data;
using Assets.net;
using Assets.utility;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.client {
    public class Client : NetHelper{
        static Client() {
            InitHanlders();
        }

        private bool isRunning = true;
        public data.ClientInfo Info { get; private set; }
        public Status Status { get; private set; }
        public UserStatus MyStatus { get { return Status.UserStatus[Info.Index]; } }

       


        public Client(String ip, data.ClientInfo info)
            : base(new TcpClient()) {
            Info = info;
            Client.Connect(ip, NetUtils.Port);
        }

        public void Loop() {
            RecvLooper.Post(() => {
                try {
                    Send(Info);
                    isRunning = true;
                    while (isRunning) {
                        try {
                            MessageContext c = MessageContext.NewRecv(this);
                            RequestDispatcher.Dispatch(c);
                        } catch (SocketException e) {
                            LogUtils.LogClient(e.GetType() + ":" + e.Message);
                        } catch (ObjectDisposedException e) {
                            LogUtils.LogClient(e.GetType() + ":" + e.Message);
                        }
                    }
                    LogUtils.LogClient("Client Finished");
                } catch (Exception e) {
                    LogUtils.LogClient("Client Crashed:" + e);
                }
            });
            
        }


        ~Client() {
            Close();
        }


        public override void Close(){
            try {
                isRunning = false;
                base.Close();
                LogUtils.LogClient("Client Shutdown");
            } catch (Exception e) {
                LogUtils.LogClient(e.Message);
            }
        }

       
        public override string ToString() {
            return Info.ToString();
        }

        /////////////////////////////////////////////

        static void InitHanlders() {
            RequestDispatcher.EnableDispatchAction();
            RequestDispatcher.Mock();
            RequestDispatcher.RegisterHandler(data.Types.GameStart, c => {
                c.Client.Info = c.GetReq<ClientInfo>(0);
                RequestDispatcher.Log(c, "GameStart,UserIndex:" + c.Client.Info.Index);
                c.Response();
            });
            RequestDispatcher.RegisterHandler(data.Types.SyncStatus, c => {
                var status = c.GetReq<data.Status>();
                if (status != null) c.Client.Status = status;
                c.Response();
            });
        }
    }
}
