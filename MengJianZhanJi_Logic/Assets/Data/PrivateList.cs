using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Data {
    public interface IPrivateList<ListType> {
        int Count { get; set; }
        ListType List { get; set; }
    }
    public class BasePrivateList<ListType,T> : ICloneable, IPrivateList<ListType> where ListType :class, ICollection<T>, IEnumerable<T>, new() {
        private ListType list;

        public virtual ListType List { get { return hide ? null : list ?? (list = new ListType()); } set { if (!hide) list = value; } }
        public virtual bool Hide { get { return hide; } set { realCount = list!=null?list.Count:realCount; hide = value; } }
        private bool hide = false;

        public BasePrivateList(ListType source) {
            List = source;
        }

        public virtual int Count {
            get {
                return List != null ? realCount = List.Count : realCount;
            }
            set {
                realCount = value;
            }
        }
        private int realCount;


        public void Add(T item) {
            if (List == null) List = new ListType();
            List.Add(item);
        }

        public void AddRange(IEnumerable<T> items) {
            if (List == null) List = new ListType();
            foreach (var e in items) Add(e);
        }

        public void Clear() {
            if (List == null) return;
            List.Clear();
        }

        public bool Contains(T item) {
            if (List == null) return false;
            return List.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            if (List == null) return;
            List.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item) {
            if (List == null) return false;
            return List.Remove(item);
        }

        public object Clone() {
            return MemberwiseClone();
        }

        public bool IsEmpty() {
            return list == null || !list.GetEnumerator().MoveNext();
        }

        public BasePrivateList<ListType, T> Clone(bool hide) {
            var c = Clone() as BasePrivateList<ListType, T>;
            c.Hide = hide;
            return c;
        }
    }
    [ProtoContract]
    public class PrivateList<T> : BasePrivateList<List<T>, T> {
        public PrivateList() : base(null) { }
        public PrivateList(List<T> source) : base(source) { }
        public T this[int index] {
            get {
                if (List == null) return default(T);
                return List[index];
            }
            set {
                if (List != null) List[index] = value;
            }
        }

        [ProtoMember(1)]
        public override int Count { get { return base.Count; } set { base.Count = value; } }
        [ProtoMember(2)]
        public override bool Hide { get { return base.Hide; } set { base.Hide = value; } }
        [ProtoMember(3)]
        public override List<T> List {
            get { return base.List; }
            set { base.List = value; }
        }

        public static implicit operator PrivateList<T>(List<T> source) {
            return new PrivateList<T>(source);
        }

        public static implicit operator List<T>(PrivateList<T> source) {
            return source.List;
        }

        public new PrivateList<T> Clone(bool hide) {
            return base.Clone(hide) as PrivateList<T>;
        }

       

    }
    [ProtoContract]
    public class PrivateLinkedList<T> : BasePrivateList<LinkedList<T>, T> {
        public PrivateLinkedList() : base(null) { }
        public PrivateLinkedList(LinkedList<T> source) : base(source) { }

        [ProtoMember(1)]
        public override int Count { get { return base.Count; } set { base.Count = value; } }
        [ProtoMember(2)]
        public override bool Hide { get { return base.Hide; } set { base.Hide = value; } }
        [ProtoMember(3)]
        public override LinkedList<T> List { get { return base.List; } set { base.List = value; } }

        public new PrivateLinkedList<T> Clone(bool hide) {
            return base.Clone(hide) as PrivateLinkedList<T>;
        }

        public static implicit operator PrivateLinkedList<T>(LinkedList<T> source) {
            return new PrivateLinkedList<T>(source);
        }

        public static implicit operator LinkedList<T>(PrivateLinkedList<T> source) {
            return source.List;
        }
    }
}
