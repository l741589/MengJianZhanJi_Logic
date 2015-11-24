using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Util {

    public interface ILogUtils {
        void LogServer(String s);

        void LogClient(String s);

        void LogSystem(String s);
    }

    public static class LogUtils {
        public static ILogUtils Impl;
        public static void LogServer(String s) {
            Impl.LogServer(s);
        }

        public static void LogClient(String s) {
            Impl.LogClient(s);
        }

        public static void LogSystem(String s) {
            Impl.LogSystem(s);
        }
    }
}
