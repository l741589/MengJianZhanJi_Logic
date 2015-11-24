using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Data {
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
    public class ListAdapter<T> {
        [ProtoMember(1)]
        public List<T> List;

        public ListAdapter() {}

        public ListAdapter(IEnumerable<T> list) {
            List = new List<T>(list);
        }
    }

    [ProtoContract]
    public class TypeAdapter<T> {
        [ProtoMember(1)]
        public T Value;

        public static implicit operator TypeAdapter<T>(T val) {
            return new TypeAdapter<T>(val);
        }

        public static implicit operator T(TypeAdapter<T> val) {
            return val.Value;
        }

        public TypeAdapter() {

        }

        public TypeAdapter(T val) {
            Value = val;
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

        public String this[String key] {
            get {
                if (valueUpdated) {
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
            return "{" + String.Join(", ", Value) + "}";
        }
    }
}
