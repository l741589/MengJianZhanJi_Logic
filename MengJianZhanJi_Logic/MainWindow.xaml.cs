using Assets.Net;
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
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            Loom.Window = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            NetHelper.SetUpServer();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            NetHelper.Shutdown();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            NetHelper.Join("127.0.0.1", new Assets.Net.Data.ClientInfo { Name="User"});
        }
    }
}
