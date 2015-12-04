﻿using Assets.Data;
using Assets.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.NetClient {
    public class MessageContext {
        public Client Client;
        public RequestHeader RequestHeader;
        public object[] RequestBody;
        public ResponseHeader ResponseHeader;
        public object[] ResponseBody;

        public Data.UserStatus MyStatus { get { return Client.MyStatus; } }
        public Data.Status Status { get { return Client.Status; } }
        public Data.ClientInfo Info { get { return Client.Info; } }
        

        public static MessageContext NewRecv(Client client) {
            var c = new MessageContext(client);
            c.Recv();
            return c;
        }

        public MessageContext(Client client) {
            Client = client;
        }

        public void Recv() {
            RequestHeader = Client.Recv<Data.RequestHeader>();
            LogUtils.LogClient(RequestHeader.ToString());
            var types = RequestHeader.BodyTypes;
            if (types != null) {
                int count = types.Count();
                RequestBody = new object[count];
                for (int i = 0; i < count; ++i) {
                    Type type = Type.GetType(types[i]);
                    RequestBody[i] = Client.Recv(type);
                }
            } else {
                RequestBody = new object[0];
            }
        }


        public void Response(params object[] responseBody) {
            ResponseHeader = new Data.ResponseHeader {
                Type = RequestHeader.Type,
                BodyTypes = responseBody.Select(e => e.GetType().FullName).ToList(),
                Sid = RequestHeader.Sid,
                Time = DateTime.Now.Ticks
            };
            ResponseBody = responseBody;
            Client.Send(ResponseHeader);
            if (responseBody != null) {
                foreach (var e in responseBody)
                    Client.Send(e);
            }
        }

        public T ReqBody<T>(int index = 0) where T :class{
            if (index >= RequestBody.Length) return null;
            var o = RequestBody[index];
            return o as T;
        }
    }
}
