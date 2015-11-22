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

        public static void RunAsync(Action a) {
            new Thread(()=>a()).Start();
        }

        public static void QueueOnMainThread(Action a) {
            Window.Dispatcher.Invoke(a);
        }
    }
}
