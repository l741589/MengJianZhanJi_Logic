using Assets.Util;
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
    public static class NetHelper {

        static public int Port { get { return 32678; } }

        static private Server server;
        static private Client client;
        static private Thread clientThread;

        static public void SetUpServer() {
            if (server!=null) return;
            server = new Server(() => {
                Join("127.0.0.1", new Data.ClientInfo { Name = "Host" });
            });
        }

        static public void Join(String addr,Data.ClientInfo info) {
            if (client != null) return;
            clientThread=Loom.RunAsync(() => {
                try { 
                    client = new Client(addr,info);
                    client.Loop();
                } catch(Exception e) {
                    LogUtils.LogClient("Client Crashed:"+e);
                    client = null;
                }
            });
        }

        static public void Shutdown() {
            if (server != null) {
                server.Close();
                server = null;
            }
            if (client != null) {
                client.Close();
                client = null;
            }
            if (clientThread != null) {
                clientThread.Interrupt();
            }
        }

        internal static void Start() {
            if (server!=null) server.Start();
        }

        static public void Recv(Socket sock, int len, Action<byte[]> a) {
            byte[] buf = new byte[len];
            Recv(sock, buf, 0, len, a);
        }

        private static int Recv(Socket sock, byte[] buf, int start, int length, Action<byte[]> a) {
            sock.BeginReceive(buf, start, length, SocketFlags.None, ar => {
                int len = sock.EndReceive(ar);
                if (len == -1) a(null);
                else if (length == len) a(buf);
                else if (len < length) start = Recv(sock, buf, start + len, length - len, a);
            }, null);
            return start;
        }

        public static void RecvInt(Socket sock,Action<int> a) {
            Recv(sock, 4, bs => a(BitConverter.ToInt32(bs,0)));
        }

        public static void RecvWithLen(Socket sock, Action<byte[]> a) {
            RecvInt(sock, len => Recv(sock, len, bs => a(bs)));
        }

        public static void RecvString(Socket sock, Action<String> a) {
            RecvWithLen(sock, bs => a(Encoding.UTF8.GetString(bs)));
        }

        public static void RecvProtoBuf<T>(Socket sock, Action<T> a) {
            RecvWithLen(sock, bs => a(Serializer.Deserialize<T>(new MemoryStream(bs))));
        }
        public static void Recv<T>(Socket sock, Action<T> a) {
            RecvProtoBuf<T>(sock, a);
        }
        static public byte[] Recv(Socket sock, int length) {
            byte[] bs = new byte[length];
            int start = 0;
            while (start < length) {
                int len = sock.Receive(bs, start, length - start, SocketFlags.None);
                start += len;
            }
            return bs;
        }

        static public int RecvInt(Socket sock) {
            return BitConverter.ToInt32(Recv(sock, 4),0);
        }

        static public byte[] RecvWithLen(Socket sock) {
            return Recv(sock, RecvInt(sock));
        }

        static public string RecvString(Socket sock) {
            return Encoding.UTF8.GetString(RecvWithLen(sock));
        }

        static public T RecvProtoBuf<T>(Socket sock){
            return Serializer.Deserialize<T>(new MemoryStream(RecvWithLen(sock)));
        }
        public static T Recv<T>(Socket sock) {
            return RecvProtoBuf<T>(sock);
        }

        public static void Send(Socket sock, byte[] data,Action a) {
            Send(sock, data, 0,data.Length, a);
        }

        private static void Send(Socket sock, byte[] data, int start,int length, Action a) {
            sock.BeginSend(data, start, length, SocketFlags.None, ar => {
                int len = sock.EndSend(ar);
                if (length == len) a();
                Send(sock, data, start + len,length-len, a);
            }, null);
        }

        public static void SendInt(Socket sock, int data, Action a) {
            Send(sock, BitConverter.GetBytes(data), a);
        }

        public static void SendWithLen(Socket sock, byte[] data, Action a) {
            SendInt(sock, data.Length, () => Send(sock, data, a));
        }

        public static void SendString(Socket sock, String s, Action a) {
            SendWithLen(sock, Encoding.UTF8.GetBytes(s), a);
        }

        public static void SendProtoBuf<T>(Socket sock, T data, Action a) {
            MemoryStream os=new MemoryStream();
            Serializer.Serialize<T>(os,data);
            SendWithLen(sock, os.ToArray(), a);
        }

        public static void Send<T>(Socket sock, T data, Action a) {
            SendProtoBuf<T>(sock, data, a);
        }

        public static void Send(Socket sock, byte[] data) {
            int length = data.Length;
            int start = 0;
            while (start < length) {
                start+=sock.Send(data, start, length - start, SocketFlags.None);
            }
        }

        public static void Send(Socket sock, int x) {
            Send(sock, BitConverter.GetBytes(x));
        }

        public static void SendWithLen(Socket sock, byte[] data) {
            Send(sock, data.Length);
            Send(sock, data);
        }

        public static void SendString(Socket sock, String s) {
            SendWithLen(sock, Encoding.UTF8.GetBytes(s));
        }

        public static void SendProtoBuf<T>(Socket sock, T data) {
            MemoryStream os = new MemoryStream();
            Serializer.Serialize<T>(os, data);
            SendWithLen(sock, os.ToArray());
        }

        public static void Send<T>(Socket sock, T data) {
            SendProtoBuf<T>(sock, data);
        }

    }
}
