using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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

        public static void Assert(bool condition,String message="") {
            if (condition) return;
            throw new AssertException(message);
        }
    }

    [Serializable]
    internal class AssertException : Exception {
        public AssertException() {
        }

        public AssertException(string message) : base(message) {
        }

        public AssertException(string message, Exception innerException) : base(message, innerException) {
        }

        protected AssertException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
