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

using Emgu.Util;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
//File directory operation
using System.IO;
using GoodsRecognitionSystem.FeatureLearning;
using GoodsRecognitionSystem.ToolKits;
using GoodsRecognitionSystem.ToolKits.SURFMethod;
using ImageManipulationExtensionMethods;
namespace GoodsFeatureLearningSystemApp
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        DirectoryInfo dir;
        FeatureLearning learningSys;
        Image<Bgr, Byte> loadImg;
        SURFFeatureData surf;
        string featureDataFilePath;
        public MainWindow()
        {
            InitializeComponent();
            dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            featureDataFilePath = dir.Parent.Parent.Parent.Parent.FullName;
            Console.WriteLine("\nfeatureDataFilePath=>" + featureDataFilePath);
        }

        private void loadTemplateImg_Click(object sender, RoutedEventArgs e)
        {
            string loadImgFileName = OpenLearningImgFile();
            if(loadImgFileName!=null)
            {
                loadImg = new Image<Bgr,byte>(loadImgFileName);
                if(learningSys !=null )
                    learningSys.SetLearningImage(loadImgFileName);
                else 
                    learningSys = new FeatureLearning(loadImgFileName);
                LoadImgViewer.Source = loadImg.ToBitmapSource();
            }
        }
        
        #region 開檔存檔
        private string OpenLearningImgFile()
        {
            string loadTemplateImgPath = featureDataFilePath + @"\GoodsRecognitionSystem\TemplateImages";
            if (File.Exists(loadTemplateImgPath))
                MessageBox.Show("路徑錯誤");
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            //移動上層在指定下層路徑
            dlg.RestoreDirectory = true;
            dlg.InitialDirectory = loadTemplateImgPath;
            dlg.Title = "Open Image File";
            dlg.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif|Png Image|*.png";
            Nullable<bool> result = dlg.ShowDialog();
            // Display OpenFileDialog by calling ShowDialog method ->ShowDialog()
            // Get the selected file name and display in a TextBox
            if (result == true && dlg.FileName != "")
            {
                // Open document
                string filename = dlg.FileName;
                return filename;
            }
            else
            {
                return null;
            }
        }
        private void SaveSURFFeatureFile(SURFFeatureData surf)
        {
            string saveSURFDataPath = featureDataFilePath + @"\GoodsRecognitionSystem\FeatureDataFiles\SURFFeatureData";
            if (File.Exists(saveSURFDataPath))
                MessageBox.Show("路徑錯誤");
            // Displays a SaveFileDialog so the user can save the Image
            // assigned to Button2.
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog(); //WPF
            dlg.Filter = "XML Files (*.xml)|*.xml";
            dlg.Title = "Save Descriptor to File";
            dlg.RestoreDirectory = true;
            dlg.InitialDirectory = saveSURFDataPath;
            Nullable<bool> result = dlg.ShowDialog();
            // If the file name is not an empty string open it for saving.
            if (result == true && dlg.FileName != "" && learningSys != null)
            {
                bool isOk = learningSys.SaveSURFFeatureData(dlg.FileName, surf);
                if (isOk) MessageBox.Show("Save SURFFeatureData Ok");
                else MessageBox.Show("Save SURFFeatureData Faild");
            }
        }
        /*
        private void SaveHistogramFile()
        {
            // Displays a SaveFileDialog so the user can save the Image

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog(); //WPF
            //SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "XML Files (*.xml)|*.xml";
            dlg.Title = "Save Histogram to File";

            //dir.Parent.Parent.FullName取得對路徑目錄
            //InitialDirectory要設置對路徑目錄
            dlg.InitialDirectory = dir.Parent.Parent.FullName + @"\FeartureDataFile\HistogramDataFile";
            dlg.RestoreDirectory = true;
            Nullable<bool> result = dlg.ShowDialog();
            // If the file name is not an empty string open it for saving.
            if (result == true && dlg.FileName != "" && learningSys != null)
            {
                bool isOk = learningSys.SaveHistogram(dlg.FileName);
                if (isOk) MessageBox.Show("Save HistogramData Ok");
                else MessageBox.Show("Save HistogramData Faild");
            }
        }
          */
	    #endregion


        private void SetUpGoodsDataButton_Click(object sender, RoutedEventArgs e)
        {
           
        }

       
        private void CalSURFDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (learningSys != null) 
            {
                SURFFeatureCanvas.Children.Clear();
                surf = learningSys.CalSURFFeature();
                Image<Bgr, Byte> drawKeyPointImg = learningSys.DrawSURFFeature(surf, loadImg);

                //放置到Canvas上,透過Image
                System.Windows.Controls.Image drawKeyPointImage = new System.Windows.Controls.Image();
                drawKeyPointImage.Width = 320;
                drawKeyPointImage.Height = 240;
                drawKeyPointImage.Source = drawKeyPointImg.ToBitmapSource();
                SURFFeatureCanvas.Children.Add(drawKeyPointImage);
            }
        }

        private void SaveSURFFeatureButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSURFFeatureFile(surf);
        }
       
    }
}
