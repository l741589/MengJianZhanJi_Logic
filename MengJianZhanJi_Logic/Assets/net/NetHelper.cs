using Assets.data;
using Assets.client;
using Assets.server;
using Assets.utility;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.net {

    public static class NetUtils {
        public static int Port = 32789;

        public static Server Server { get; private set; }
        public static Client Client { get; private set; }

        public static void SetUpServer() {
            if (Server != null) return;
            Server = new Server(() => Join("127.0.0.1", "Host"));
        }

        public static void Join(String ip, String name) {
            if (Client != null) return;
            Client = new Client(ip, new ClientInfo { Name = name });
            Client.Loop();
        }

        public static void Shutdown() {
            if (Client != null) Client.Close();
            if (Server != null) Server.Close();
            Client = null;
            Server = null;            
        }

    }

    public class NetHelper {

        public Looper RecvLooper { get; private set; }
        public Looper SendLooper { get; private set; }
        public TcpClient Client { get; private set;}
        private NetworkStream Stream { get { return Client.GetStream(); } }

        public NetHelper(TcpClient client,Looper SendLoop = null,Looper RecvLoop = null) {
            Client = client;
            if (RecvLoop == null) {
                RecvLooper = new Looper();
                RecvLooper.StartNewThread("RecvLooper");
            } else {
                RecvLooper = RecvLoop;
            }
            if (SendLoop == null) {
                SendLooper = new Looper();
                SendLooper.StartNewThread("SendLooper");
            } else {
                SendLooper = SendLoop;
            }
        }

        private static MethodInfo serializerDeserializeMethod;
        static public object DeserializeProtoBuf(byte[] bs, Type type) {
            if (serializerDeserializeMethod == null) {
                serializerDeserializeMethod = typeof(Serializer).GetMethod("Deserialize");
            }
            var m = serializerDeserializeMethod.MakeGenericMethod(type);
            return m.Invoke(null, new object[] { new MemoryStream(bs) });
        }


        public static byte[] SerializeProtobuf<T>(T obj) {
            MemoryStream os = new MemoryStream();
            Serializer.Serialize(os, obj);
            return os.ToArray();
        }

        public void Send(byte[] data) {
            SendLooper.Send(() => Stream.Write(data, 0, data.Length));
        }

        public void Send(int x) {
            Send(BitConverter.GetBytes(x));
        }

        public void SendWithLen(byte[] data) {
            Send(data.Length);
            Send(data);
        }  

        public void Send<T>(T protobuf) {
            SendWithLen(SerializeProtobuf(protobuf));
        }

        public void SendAsync(byte[] data) {
            SendLooper.Post(() => Stream.Write(data, 0, data.Length));
        }

        public void SendAsync(int x) {
            SendAsync(BitConverter.GetBytes(x));
        }

        public void SendWithLenAsync(byte[] data) {
            SendAsync(data.Length);
            SendAsync(data);
        }

        public void SendAsync<T>(T protobuf) {
            SendWithLenAsync(SerializeProtobuf(protobuf));
        }

        public byte[] Recv(int length) {
            return RecvLooper.Send(() => {
                byte[] bs = new byte[length];
                int start = 0;
                while (start<length) {
                    int len = Stream.Read(bs, start, length - start);
                    if (len == -1) {
                        byte[] truncData = new byte[len];
                        Array.Copy(bs,truncData, len);
                        return truncData;
                    }
                    start += len;
                }
                return bs;
            });            
        }

        public int RecvInt() {
            return BitConverter.ToInt32(Recv(4), 0);
        }

        public byte[] RecvWithLen() {
            int len = RecvInt();
            return Recv(len);
        }

        public object Recv(Type protobufType) {
            byte[] bs = RecvWithLen();
            return DeserializeProtoBuf(bs, protobufType);
        }

        public T Recv<T>() {
            return (T)Recv(typeof(T));
        }


        public virtual void Close() {
            Client.Close();
            RecvLooper.Stop();
            SendLooper.Stop();
        }
    }
}
