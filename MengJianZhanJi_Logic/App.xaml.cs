using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace MengJianZhanJi_Logic {
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application {
        public static String UserName;
        public static bool IsClient;
        private void Application_Startup(object sender, StartupEventArgs e) {
            if (e.Args.Length > 0) {
                UserName = e.Args[0];
                IsClient = true;
            } else {
                IsClient = false;
                UserName = "Host";
            }
        }
    }
}
