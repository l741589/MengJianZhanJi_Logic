using MengJianZhanJi_Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace UnityEngine {
    public static class Loom {
        public static MainWindow Window { get; set; }

        public static Thread RunAsync(Action a) {
            var t = new Thread(() => a());
            t.Start();
            return t;
        }

        public static void QueueOnMainThread(Action a) {
            Window.Dispatcher.Invoke(a);
        }
    }
}
