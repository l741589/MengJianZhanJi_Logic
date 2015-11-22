using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEngine {
    public static class Debug {
        public static void Log(String s){
            Console.WriteLine(s);
            //System.Diagnostics.Debug.WriteLine("[INFO]"+s);
        }
        public static void LogError(String s) {
            Console.WriteLine(s);
            //System.Diagnostics.Debug.WriteLine("[ERROR]"+s);
        }
    }
}
