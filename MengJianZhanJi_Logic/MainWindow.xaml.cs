using Assets.Net;
using Assets.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UnityEngine;

namespace MengJianZhanJi_Logic {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window,ILogUtils {
        public MainWindow() {
            InitializeComponent();
            Loom.Window = this;
            Debug.W = this;
            LogUtils.Impl = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            NetHelper.SetUpServer();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            NetHelper.Shutdown();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            NetHelper.Join("127.0.0.1", new Assets.Data.ClientInfo { Name="User"});
        }

        private void Button_Click_3(object sender, RoutedEventArgs e) {
            NetHelper.Start();
        }

        public void LogServer(string s) {
            
            this.Dispatcher.Invoke(new Action(() => {
                if (!tb_logServer.IsVisible) {
                    tb_logServer.Visibility = Visibility.Visible;
                    Grid.SetColumn(tb_logClient, 2);
                    Grid.SetColumnSpan(tb_logClient, 1);
                }
                tb_logServer.AppendText(s + "\n");
                tb_logServer.ScrollToEnd();
            }));
        }

        public void LogClient(string s) {            
            this.Dispatcher.Invoke(new Action(() => {
                tb_logClient.AppendText(s + "\n");
                tb_logClient.ScrollToEnd();
            }));
        }

        public void LogSystem(string s) {
            Console.WriteLine(s);/*
            this.Dispatcher.Invoke(new Action(() => {
                tb_logSystem.AppendText(s + "\n");
            }));*/
        }

        private void Button_Click_4(object sender, RoutedEventArgs e) {
            tb_logClient.Clear();
            tb_logServer.Clear();
        }

        private void Button_Click_5(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start(AppDomain.CurrentDomain.BaseDirectory + "MengJianZhanJi_Logic.exe");
        }

        protected override void OnClosed(EventArgs e) {
            NetHelper.Shutdown();
            base.OnClosed(e);            
        }
    }


}
