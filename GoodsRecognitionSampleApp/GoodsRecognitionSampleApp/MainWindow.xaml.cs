using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.Util;
using GoodsRecognitionSystem;
using GoodsRecognitionSystem.ToolKits;
using ImageManipulationExtensionMethods;
namespace GoodsRecognitionSampleApp
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        GoodsRecognition goodsRecogSys;
        
        private KinectSensor sensor;
        private WriteableBitmap colorBitmap;
        private int colorBitmapStride;
        private byte[] colorPixels;
        private ColorImageFrame colorFrame;
        private ColorImagePoint[] colorPoints;

        private WriteableBitmap depthBitmap;
        private int depthBitmapStride;
        private short[] depthPixels; //新版寫法-> DepthImagePixel[]
        private DepthImageFrame depthFrame;

        bool isCombineDepthToColor;
        bool isZKeyDown;
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            this.Unloaded += new RoutedEventHandler(MainWindow_Unloaded);
            this.KeyDown +=MainWindow_KeyDown;
            this.KeyUp +=MainWindow_KeyUp;
            isZKeyDown = false;
            isCombineDepthToColor = false;
            
            try
            {
                //此部分程式使用時要擺放在取得影像區塊
                //-----------
                Image<Bgr, Byte> observedImg = new Image<Bgr, byte>("..\\..\\test.png");
                
                if (goodsRecogSys != null)
                    goodsRecogSys.SetupInputImage(observedImg);
                else
                    goodsRecogSys = new GoodsRecognition(observedImg);

                string goodData = goodsRecogSys.RunRecognition(true);
                MessageBox.Show("商品資訊:" + goodData);
                //-----------
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    sensor = potentialSensor;
                    break;
                }
            }
            if (sensor != null)
            {
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];
                colorBitmapStride = sensor.ColorStream.FrameWidth * sensor.ColorStream.FrameBytesPerPixel;
                colorBitmap = new WriteableBitmap(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                
                // Allocate space to put the depth pixels we'll receive
                depthPixels = new short[sensor.DepthStream.FramePixelDataLength];
                // This is the bitmap we'll display on-screen
                depthBitmap = new WriteableBitmap(sensor.DepthStream.FrameWidth,
                    sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Gray16, null);
                depthBitmapStride = sensor.DepthStream.FrameWidth * sensor.DepthStream.FrameBytesPerPixel;
                // Turn on the depth stream to receive depth frames
                sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                sensor.AllFramesReady += sensor_AllFramesReady;

                sensor.Start();

            }
        }

        private void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            depthFrame = e.OpenDepthImageFrame();
            colorFrame = e.OpenColorImageFrame();

            if (depthFrame != null && colorFrame != null)
            {
                depthFrame.CopyPixelDataTo(depthPixels);
                colorFrame.CopyPixelDataTo(colorPixels);

                Image<Bgr, Byte> convertImage = colorFrame.ToOpenCVImage<Bgr, Byte>();

                depthBitmap.WritePixels(new Int32Rect(0, 0, sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight), depthPixels, depthBitmapStride, 0);

                if (depthPixels != null && isCombineDepthToColor)
                     RangeFilter();

                colorBitmap.WritePixels(new Int32Rect(0, 0, sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight), colorPixels, colorBitmapStride, 0);
                colorImageViewer.Source = colorBitmap;
                depthImageViewer.Source = depthBitmap;

                //depthFrame.Dispose();
                //colorFrame.Dispose();
            }
        }
        #region 校正深度與影像的畫面函示
        //////////////////////////////////////////////////////////////////////////////////////////////////////
        private void RangeFilter()
        {
            int min = sensor.DepthStream.MinDepth;
            int max = sensor.DepthStream.MaxDepth;

            colorPoints = new ColorImagePoint[depthPixels.Length];
            //CoordinateMapper map = new CoordinateMapper(sensor); //新版寫法
            //map.MapDepthFrameToColorFrame(depthFrame.Format, depthPixels, colorFrame.Format, colorPoints);

            sensor.MapDepthFrameToColorFrame(depthFrame.Format, depthPixels, colorFrame.Format, colorPoints);

            for (int i = 0; i < depthPixels.Length; i++)
            {
                PixelInRange(i, min, max);
            }
        }
        private void PixelInRange(int i, int min, int max)
        {
            int depth = depthPixels[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
            if (depth < min || depth > 1000) //調整距離
            {
                ColorImagePoint p = colorPoints[i];
                int colorindex = (p.X + p.Y * colorFrame.Width) * colorFrame.BytesPerPixel;
                colorPixels[colorindex] = 0x00; //顏色
                colorPixels[colorindex + 1] = 0x00;
                colorPixels[colorindex + 2] = 0x00;
            }
        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
 
        }
        private void combineDepthToImgButton_Click(object sender, RoutedEventArgs e)
        {
            if (isCombineDepthToColor) isCombineDepthToColor = false;
            else isCombineDepthToColor = true;
        }

        #region 鍵盤按鍵事件,只是為了擷取影像用
        //////////////////////////////////////////////////////////////////////////////////////////////////////
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z)
            {
                isZKeyDown = true;
            }
        }
        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z && isZKeyDown)
            {
                MessageBox.Show("key down");
                isZKeyDown = false;
            }

        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
    }
}
