//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Windows.Shapes;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using Emgu.Util;
    using Emgu.CV.UI;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Features2D;
    using GoodsRecognitionSystem;
    using GoodsRecognitionSystem.ToolKits;
    using System.Drawing;
    using System.Drawing.Imaging;
    using ImageManipulationExtensionMethods;
    using System.Runtime.InteropServices;
    using System.Windows.Media;
    using Microsoft.VisualBasic;
    using Microsoft.VisualBasic.Devices;
    using System.Windows.Threading;
    using System.Windows;
    using System.IO.Ports;
    using System.Threading;
    using System.Speech.Synthesis;
    using ImageManipulationExtensionMethods;
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;
        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap2;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// </summary>
        private short[] depthPixels;

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorPixels;
        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorPixels2;
        private byte[] colorPixels3;//深度影像資料
        Image<Gray, Byte> image2 = new Image<Gray, Byte>(640, 480);
        Image<Bgr, Byte> color_image = new Image<Bgr, Byte>(640, 480);//物品辨識彩色影像
        Image<Gray, Byte> test = new Image<Gray, Byte>(640, 480);
        //Timer timer = new Timer();
        private SerialPort serialPort1 = new SerialPort("COM3", 9600);
        //private SerialPort serialPort2 = new SerialPort("COM3", 9600);
        // private int state=99;
        bool ck;
        GoodsRecognition goodsRecogSys;
        SpeechSynthesizer synthesizer = new SpeechSynthesizer() { Rate = 0, Volume = 100 };
        //GoodsRecognition goodsRecogSys;
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
     
        public MainWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit


            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
                //this.Image2.Source = this.colorBitmap;//彩色影像顯示
                this.image1.Source = this.colorBitmap;//彩色影像顯示

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;
               
                // Turn on the depth stream to receive depth frames
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                // Allocate space to put the depth pixels we'll receive
                this.depthPixels = new short[this.sensor.DepthStream.FramePixelDataLength];

                // Allocate space to put the color pixels we'll create
                this.colorPixels2 = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];
                this.colorPixels3 = new byte[this.sensor.DepthStream.FramePixelDataLength];//灰階影像儲存

                // This is the bitmap we'll display on-screen
                this.colorBitmap2 = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                
                //string serialInByte = serialPort1.ReadLine();
               
               // int a_pic =serialPort1.ReadChar();
               // if (test '17') { ck = true; }
                // Set the image we display to point to the bitmap where we'll put the image data
                //this.Image.Source = this.colorBitmap2;//深度影像顯示
                // Add an event handler to be called whenever there is new depth frame data
                this.sensor.DepthFrameReady += this.SensorDepthFrameReady;
                
                // Start the sensor!
                try
                {
                    this.sensor.Start();
                    serialPort1.Open();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
                serialPort1.Write("0");
                serialPort1.Close();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);
                    Image<Bgr, Byte> image2 = colorFrame.ToOpenCVImage<Bgr, Byte>();//將kinect彩色影像轉換emgu處理格式
                    Image<Gray, Byte> grayFrame = image2.Convert<Gray, Byte>();
                    Image<Gray, Byte> cannyFrame = grayFrame.Canny(new Gray(100), new Gray(60));
                    //image1.Source = cannyFrame.ToBitmapSource(); //將emgu格式轉換bitmap顯示
                    if (ck == true) 
                    {
                        ck = false;
                        if (goodsRecogSys != null)
                            goodsRecogSys.SetupInputImage(image2);
                        else
                            goodsRecogSys = new GoodsRecognition(image2);

                        string goodData = goodsRecogSys.RunRecognition(true);
                        if (synthesizer.State != SynthesizerState.Speaking)
                        {
                            synthesizer.SpeakAsync(goodData);
                        }
                       
                    }
                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {    
                if (depthFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyPixelDataTo(this.depthPixels);

                    
                    short depth2 = (short)(this.depthPixels[153280] >> DepthImageFrame.PlayerIndexBitmaskWidth);
                    // Convert the depth to RGB
                    int colorPixelIndex = 0;
                    int num = 0;
                    for (int i = 0; i < this.depthPixels.Length; ++i)
                    {
                        // discard the portion of the depth that contains only the player index
                        short depth = (short)(this.depthPixels[i] >> DepthImageFrame.PlayerIndexBitmaskWidth);
                       
                        // to convert to a byte we're looking at only the lower 8 bits
                        // by discarding the most significant rather than least significant data
                        // we're preserving detail, although the intensity will "wrap"
                        // add 1 so that too far/unknown is mapped to black
                        byte intensity = (byte)((depth + 1) & byte.MaxValue);
                        byte gray = (byte)(255-depth / 15.7);
                        // Write out blue byte
                        this.colorPixels2[colorPixelIndex++] = gray;

                        // Write out green byte
                        this.colorPixels2[colorPixelIndex++] = gray;

                        // Write out red byte                        
                        this.colorPixels2[colorPixelIndex++] = gray;

                        if (depth <= 1500)
                        {
                            this.colorPixels3[i] = gray;
                            //num++;
                            
                        }
                        else 
                        {
                            this.colorPixels3[i] =255;
                        }
                        // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                        // If we were outputting BGRA, we would write alpha here.
                        ++colorPixelIndex;
                    }
                    
                    // Write the pixel data into our bitmap
                    this.colorBitmap2.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap2.PixelWidth, this.colorBitmap2.PixelHeight),
                        this.colorPixels2,
                        this.colorBitmap2.PixelWidth * sizeof(int),
                        0);

                    image2.Bytes = this.colorPixels3;
                    test.Bytes = this.colorPixels3;
                    image2._Erode(3);
                    test._Erode(3);
                    Image<Gray, Byte> cannyFrame = image2.Canny(new Gray(100), new Gray(60));
                    //Image<Gray, Byte> cannyFrame2 = test.Canny(new Gray(100), new Gray(60));
                    //image1.Source = image2.ToBitmapSource();
                    //image3.Source = cannyFrame.ToBitmapSource();

                    image3.Source = image2.ToBitmapSource();

                    test.ROI = new System.Drawing.Rectangle(150, 0, 340, 160);
                    cannyFrame.ROI = new System.Drawing.Rectangle(150, 0, 340, 160);
                    Contour<System.Drawing.Point> cont = cannyFrame.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_CCOMP, new MemStorage());
                    for (; cont != null; cont = cont.HNext)
                    {
                        num++;
                    }


                    if (num >= 3) { label2.Content = "注意頭部"; Speak(3); } else { label2.Content = "偵測中"; }
                    image4.Source = test.ToBitmapSource();

                    test.ROI = new System.Drawing.Rectangle(150, 160, 340, 160);
                    cannyFrame.ROI = new System.Drawing.Rectangle(150, 160, 340, 160);
                    Contour<System.Drawing.Point> cont2 = cannyFrame.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_CCOMP, new MemStorage());
                    int num2 = 0;
                    for (; cont2 != null; cont2 = cont2.HNext)
                    {
                        num2++;
                    }
                    if (num2 >= 3) { label3.Content = "注意胸部";
                        //Speak(4);
                    } else { label3.Content = "偵測中"; }
                   
                    image5.Source = test.ToBitmapSource();

                    test.ROI = new System.Drawing.Rectangle(150, 320, 340, 160);
                    cannyFrame.ROI = new System.Drawing.Rectangle(150, 320, 340, 160);
                    Contour<System.Drawing.Point> cont3 = cannyFrame.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_CCOMP, new MemStorage());
                    int num3 = 0;
                    for (; cont3 != null; cont3 = cont3.HNext)
                    {
                       num3++;
                    }
                    if (num3 >= 3) { label4.Content = "注意腰部"; 
                        //Speak(5);
                    } else { label4.Content = "偵測中"; }
                    image6.Source = test.ToBitmapSource();

                    //震動警示位置
                    test.ROI = new System.Drawing.Rectangle(245, 0, 95, 480);
                    cannyFrame.ROI = new System.Drawing.Rectangle(245, 0, 95, 480);
                    Contour<System.Drawing.Point> cont4 = cannyFrame.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_CCOMP, new MemStorage());
                    int num4 = 0;
                    for (; cont4 != null; cont4 = cont4.HNext)
                    {
                        num4++;
                    }
                    //label1.Content = "左邊障礙物";
                    if (num4 >= 2) { label1.Content = "左邊「震動警示」"; }else{label1.Content = "左邊偵測中 ";}
                    test.ROI = new System.Drawing.Rectangle(150, 0, 95, 480);
                    cannyFrame.ROI = new System.Drawing.Rectangle(150, 0, 95, 480);
                    Contour<System.Drawing.Point> cont5 = cannyFrame.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_CCOMP, new MemStorage());
                    int num5 = 0;
                    for (; cont5 != null; cont5 = cont5.HNext)
                    {
                        num5++;
                    }
                    //label5.Content = "右邊障礙物";
                    if (num5 >= 2) { label5.Content = "右邊「震動警示」"; } else { label5.Content = "右邊偵測中 "; }
                    if (num4 >= 2 && num5 >= 2) //左右邊障礙物震動警示
                        
                    { 
                        serialPort1.Write("1"); //前方有障礙物
                        
                    }
                    else if (num4 >= 2) //左邊有障礙物
                    {
                        //Speak(2);
                          Speak(6);
                        
                        
                        serialPort1.Write("5");
                    }
                    else if (num5 >= 2)//右邊有障礙物 
                    {
                        serialPort1.Write("4");
                        //Speak(1);
                          Speak(7);
                    }
                    else { serialPort1.Write("0"); }
                    
                    //if (ra1 == 99) { aaaa.Content = "test ok"; }
                   
                }
            }
        }
        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        
        void Speak(int ch)
        {
            if (synthesizer.State != SynthesizerState.Speaking)
                switch (ch)
                {
                    case 1:
                        synthesizer.SpeakAsync("On the right there is an obstacle");
                        break;
                    case 2:
                        synthesizer.SpeakAsync("On the left there is an obstacle");
                        break;
                    case 3:
                        synthesizer.SpeakAsync("Warning head obstacle ahead");
                        break;
                    case 4:
                        synthesizer.SpeakAsync("Warning chest obstacle ahead");
                        break;
                    case 5:
                        synthesizer.SpeakAsync("Warning waist obstacle ahead");
                        break;
                    case 6:
                        synthesizer.SpeakAsync("Please keep right");
                        break;
                    case 7:
                        synthesizer.SpeakAsync("Please keep left");
                        break;

                }
                
        }
        private void ButtonScreenshotClick(object sender, RoutedEventArgs e)
        {
            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.ConnectDeviceFirst;
                return;
            }

            // create a png bitmap encoder which knows how to save a .png file
            BitmapEncoder encoder = new PngBitmapEncoder();

            // create frame from the writable bitmap and add to encoder
            encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            
            //string path = Path.Combine(myPhotos, "KinectSnapshot-" + time + ".png");

            // write the new file to disk
            //try
            //{
            //    using (FileStream fs = new FileStream(path, FileMode.Create))
            //    {
            //        encoder.Save(fs);
            //    }

            //    this.statusBarText.Text = string.Format("{0} {1}", Properties.Resources.ScreenshotWriteSuccess, path);
            //}
            //catch (IOException)
            //{
            //    this.statusBarText.Text = string.Format("{0} {1}", Properties.Resources.ScreenshotWriteFailed, path);
            //}
        }

        private void TackPictureButton_Click(object sender,RoutedEventArgs e)
        {

            ck = true;
        }
    }
}