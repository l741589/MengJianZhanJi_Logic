using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Assets.Net {
    public class Looper {
        public delegate object LooperAction();
        public bool IsRunning { get; private set; }
        private AutoResetEvent Event = new AutoResetEvent(false);
        private LinkedList<LooperAction> ActionQueue = new LinkedList<LooperAction>();
        public Thread Thread { get; private set; }
        public bool IsDebug { get; set; }

        public Looper() {
            IsRunning = false;
            IsDebug = true;
        }

        private void Log(string s) {
            if (IsDebug) {
                String l = Thread.Name+": "+s;
                if (Thread != Thread.CurrentThread) {
                    l = Thread.CurrentThread.Name + " => " + l;
                }
                Debug.WriteLine(l);
            }
        }

        public Thread StartNewThread(String name=null) {
            var t=new Thread(Start);
            t.Name = name;
            t.Start();
            return t;
        }

        public void Start() {
            IsRunning = true;
            Thread = Thread.CurrentThread;
            while (IsRunning) {
                bool isEmpty;
                lock (ActionQueue) {
                    isEmpty=ActionQueue.Count == 0;
                }
                if (isEmpty) {
                    Log("Wait");
                    Event.WaitOne();
                    Log("Wake");
                }
                if (!IsRunning) break;
                LooperAction a = null;
                lock (ActionQueue) {
                    if (ActionQueue.Count > 0) {
                        a = ActionQueue.First();
                        ActionQueue.RemoveFirst();
                    }
                }
                if (a != null) {
                    Log("Action");
                    try {
                        a();
                    }catch(Exception e) {
                        Log(e.Message+"\n"+e.StackTrace);
                    }
                }
            }
            Log("Finished");
        }

        public void Post(Action a) {
            if (a == null) return;
            Post(() => { a(); return null; });
        }

        public void Post(LooperAction a) {
            if (a == null) return;
            if (Thread == Thread.CurrentThread) {
                a();
            } else {
                lock (ActionQueue) {
                    ActionQueue.AddLast(a);
                    Log("Post");
                }
                Event.Set();
            }
        }

        public void Send(Action a) {
            if (a == null) return;
            Send<object>(() => { a(); return null; });
        }

        public T Send<T>(Func<T> a) {
            if (!IsRunning) throw new ThreadInterruptedException();
            if (a == null) return default(T);
            if (Thread == Thread.CurrentThread) {
                return a();
            } else {
                object ret = null;
                Exception err = null;
                AutoResetEvent E = new AutoResetEvent(false);
                Post(() => {
                    try {
                        ret = a();
                    }catch(Exception e) {
                        err = e;
                    }
                    E.Set();
                });
                E.WaitOne();                
                if (err != null) throw err;
                return (T)ret;
            }
        }

        public void Stop() {
            IsRunning = false;
            Event.Set();
        }
    }
}
