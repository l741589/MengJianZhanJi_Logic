using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Assets.Utility {
    public static class IOUtils {
        public static String ReadStringFromFile(string name) {
#if WPF
            return File.ReadAllText("Assets/"+name);
#endif
        }

        public static T[] ReadCsv<T>(string name) where T:new(){
            String text = ReadStringFromFile(name);
            String[] lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            String[] fields = lines[0].Split(new char[] { ','});
            var ss = lines.Skip(1);
            T[] ts = new T[lines.Length-1];
            int l = 0;
            int c = fields.Length;
            var type = typeof(T);
            Action<object,object>[] setters = new Action<object,object>[c];
            foreach (string field in fields) {
                var fieldname = field.Trim();
                var prop = type.GetProperty(fieldname);
                if (prop != null) {
                    setters[l++] = (self, val) => prop.SetValue(self, Cast(val, prop.PropertyType), null);
                    continue;
                }
                var f= type.GetField(fieldname);
                if (f != null) { 
                    setters[l++] = (self, val) => f.SetValue(self, Cast(val, f.FieldType));
                    continue;
                }
                setters[l++] = null;
            }
            l = 0;
            foreach (string line in ss) {
                String[] values = line.Split(new char[] { ',' });
                var t = ts[l++] = new T();
                for (int i=0;i< c; ++i) {
                    if (setters[i] != null) setters[i](t, values[i]);
                }
            }
            return ts;
        }


        public static object Cast(object value, Type type) {
            if (value is String) {
                if (typeof(string).IsAssignableFrom(type)) return value;
                if (typeof(long).IsAssignableFrom(type)) return long.Parse(value.ToString());
                if (typeof(int).IsAssignableFrom(type)) return int.Parse(value.ToString());
                if (typeof(double).IsAssignableFrom(type)) return double.Parse(value.ToString());
                if (typeof(float).IsAssignableFrom(type)) return float.Parse(value.ToString());
                if (typeof(bool).IsAssignableFrom(type)) return bool.Parse(value.ToString());
                return null;
            } else if (typeof(string).IsAssignableFrom(type)) {
                if (value == null) return "";
                return value.ToString();
            } else {
                return Cast(Cast(value, typeof(String)), type);
            }            
        }
    }

    
}
