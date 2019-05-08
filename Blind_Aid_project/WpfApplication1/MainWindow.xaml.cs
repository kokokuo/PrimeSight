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
using Microsoft.Kinect;

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            Unloaded += new RoutedEventHandler(MainWindow_Unloaded);
        }
        void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            
        }
        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            DiscoverKinectSensor();
            string info = "狀態: " + e.Status + " , 感應器ID: " + e.Sensor.UniqueKinectId + " , 連線ID: " + e.Sensor.DeviceConnectionId;
            TextBlock tb = new TextBlock()
            {
                Text = info
            };
            status.Items.Add(tb) ;
            WindowOpenAndClose(e.Status, e.Sensor);
        }

        Dictionary<string, Window> knowtable = new Dictionary<string, Window>();
        void WindowOpenAndClose(KinectStatus status, KinectSensor sensor)
        {
            switch (status)
            {
                case KinectStatus.Connected:
                    if (!knowtable.ContainsKey(sensor.DeviceConnectionId))
                    {
                        ColorWindow cw = new ColorWindow(sensor);
                        knowtable[sensor.DeviceConnectionId] = cw; //把Connection ID和視窗關聯起來
                        cw.Show();
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (knowtable.ContainsKey(sensor.DeviceConnectionId))
                    {
                        ColorWindow w = (ColorWindow)knowtable[sensor.DeviceConnectionId];
                        w.Close();
                        knowtable.Remove(sensor.DeviceConnectionId); //移除關聯
                    }
                    break;
            }
        }
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            DiscoverKinectSensor();
        }
        private void DiscoverKinectSensor()
        {
            string info = "偵測到" + KinectSensor.KinectSensors.Count + "台感應器";
            TextBlock tb = new TextBlock() { Text = info , Foreground = Brushes.Red};
            status.Items.Add(tb);

            foreach (var s in KinectSensor.KinectSensors)
            {
                string i = "偵測到感應器ID: " + s.UniqueKinectId + " , 連線ID: " + s.DeviceConnectionId + " , 狀態" + s.Status; 
                TextBlock t = new TextBlock() { Text = i };
                status.Items.Add(t);
                WindowOpenAndClose(s.Status, s);
            }
        }
    }
}
