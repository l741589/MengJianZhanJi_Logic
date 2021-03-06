﻿using Assets.net;
using Assets.utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            Loaded += MainWindow_Loaded;
        }

        private List<System.Diagnostics.Process> clientProcesses = new List<System.Diagnostics.Process>();

        void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            if (App.IsClient) {
                NetUtils.Join("127.0.0.1", App.UserName);
            } else {
                NetUtils.SetUpServer(() => {
                    for (int i = 0; i < 3; ++i) {
                        var p=System.Diagnostics.Process.Start(AppDomain.CurrentDomain.BaseDirectory + "MengJianZhanJi_Logic.exe", "Client" + i);
                        clientProcesses.Add(p);
                    }
                });
                Topmost = true;
            }
        }
        
        private void Button_Click(object sender, RoutedEventArgs e) {
            NetUtils.SetUpServer();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            NetUtils.Shutdown();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            NetUtils.Join("127.0.0.1","User");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e) {
            NetUtils.Server.Start();
            Topmost = false;
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
            NetUtils.Shutdown();
            //Thread.Sleep(5000);
            base.OnClosed(e);
            foreach (var p in clientProcesses) {
                p.Kill();
            }
        }
    }
}
