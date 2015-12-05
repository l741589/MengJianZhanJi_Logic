using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.utility {
    public static class Util {
        public static bool RemoveIf<T>(ICollection<T> c, Predicate<T> a) {
            foreach (var e in c) {
                if (a(e)) {
                    if (c.Remove(e))
                        return true;
                }
            }
            return false;
        }
    }
}
