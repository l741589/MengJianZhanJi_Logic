using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Net.Data {
    [ProtoContract]
    public class ClientInfo {
        [ProtoMember(1)]
        public int Version { get; set; }
        [ProtoMember(2)]
        public String Name { get; set; }

        public ClientInfo() {
            Version = 0x01000000;
        }
    }

    [ProtoContract]
    public class KeyValuePair {
        [ProtoMember(1)]
        public string Key { get; set; }
        [ProtoMember(2)]
        public string Value { get; set; }

        public override string ToString() {
            return '"' + Key + "\":\"" + Value + '"';
        }
    }

    [ProtoContract]
    public class Map {
        private List<KeyValuePair> value = new List<KeyValuePair>();

        [ProtoMember(1)]
        public List<KeyValuePair> Value {
            get {
                if (mapUpdated) {
                    value.Clear();
                    foreach (var p in cachedMap) value.Add(new KeyValuePair { Key = p.Key, Value = p.Value });
                    mapUpdated = false;
                }
                return value; 
            }
            set { 
                this.value = value;
                valueUpdated = true;
            }
        }

        private Dictionary<String, String> cachedMap = new Dictionary<string, string>();
        private bool valueUpdated = false;
        private bool mapUpdated = false;

        String this[String key]{
            get {
                if (valueUpdated){
                    cachedMap.Clear();
                    foreach (var p in Value) cachedMap.Add(p.Key, p.Value);
                    valueUpdated = false;
                }
                string value;
                if (cachedMap.TryGetValue(key, out value)) return value;
                return null;
            }
            set {
                cachedMap.Add(key, value);
                mapUpdated = true;
            }
        }

        public override string ToString() {
            return "{"+String.Join(", ", Value)+"}";
        }
    }

    [ProtoContract]
    public class RequestHeader {
        [ProtoMember(1)]
        public string Type { get; set; }
        [ProtoMember(2)]
        public Map Args { get; set; }
        [ProtoMember(3)]
        public int Count { get; set; }

        public override string ToString() {
            return Type + "?" + Args;
        }
    }

    [ProtoContract]
    public class ResponseHeader {
        [ProtoMember(1)]
        public string Type { get; set; }
        [ProtoMember(3)]
        public int Count { get; set; }
    }
}
