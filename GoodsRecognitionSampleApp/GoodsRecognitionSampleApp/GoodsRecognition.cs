using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.Util;
using GoodsRecognitionSystem;
using GoodsRecognitionSystem.ToolKits;
using GoodsRecognitionSystem.ToolKits.SURFMethod;
namespace GoodsRecognitionSampleApp
{
    /// <summary>
    /// 商品辨識的類別,請直接使用此類別作為執行商品辨識的接口
    /// </summary>
    public class GoodsRecognition
    {
        Image<Bgr, Byte> observedImg;
        Image<Bgr, Byte> objectImg;
        List<string> surfFiles;
        Dictionary<string, string> goodsData = new Dictionary<string, string>();
        public GoodsRecognition(Image<Bgr, Byte> observedSrcImg)
        {
            SetUpGoodsData();
            observedImg = observedSrcImg.Copy();
            //要有深度過濾才行使用此code
            Image<Bgr, Byte> dst = SkinFilter(observedImg);
            objectImg = GetObjectBoundingBoxImg(dst);
            
            //取得專案執行檔所在的目錄=>WPF:AppDomain.CurrentDomain.BaseDirectory
            //使用DirectoryInfo移動至上層
            DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            string projectPath = dir.Parent.Parent.Parent.Parent.FullName;
            //讀取surf檔案的目錄下所有檔案名稱
            Console.WriteLine("\nPath=>" + projectPath + "\n");
            //隨著此類別存放的位置不同,要重新設定檔案路徑
            if (Directory.Exists(projectPath + @"\GoodsRecognitionSystem\FeatureDataFiles\SURFFeatureData"))
            {
                surfFiles = Directory.GetFiles(projectPath + @"\GoodsRecognitionSystem\FeatureDataFiles\SURFFeatureData").ToList();
            }
            
        }
        /// <summary>
        /// 設置要作為辨識的輸入影像
        /// </summary>
        /// <param name="observedSrcImg">要比對觀察的影響</param>
        public void SetupInputImage(Image<Bgr, Byte> observedSrcImg) 
        {
            observedImg = observedSrcImg.Copy();
            //要有深度過濾才行使用此code
            Image<Bgr, Byte> dst = SkinFilter(observedImg);
            objectImg = GetObjectBoundingBoxImg(dst);
        }

        #region 取出物體所在的ROI影像
        ////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 取出ROI物體圖像
        /// </summary>
        /// <param name="filteredSkinObserved">過濾膚色的圖像</param>
        /// <returns>回傳物體圖像</returns>
        private Image<Bgr, Byte> GetObjectBoundingBoxImg(Image<Bgr, Byte> filteredSkinObserved)
        {
            //get bounding box
            Image<Gray, Byte> gray = filteredSkinObserved.Convert<Gray, Byte>();
            gray = DetectObjects.DoBinaryThreshold(gray, 0);
            gray._Erode(5);
            gray._Dilate(5);
            new ImageViewer(gray).Show();
            Contour<Point> sceneContours = DetectObjects.DoContours(gray);
            Contour<Point> objectBox = DetectObjects.GetMaxContours(sceneContours);
            Image<Bgr, Byte> boxImg = DetectObjects.GetBoundingBoxImage(objectBox, filteredSkinObserved);
            new ImageViewer(boxImg).Show();
            return boxImg;

        }
        /// <summary>
        /// 膚色過濾
        /// </summary>
        /// <param name="observed">要對得觀察景象</param>
        /// <returns>回傳過濾掉的膚色</returns>
        private Image<Bgr, Byte> SkinFilter(Image<Bgr, Byte> observed)
        {
            Image<Bgr, Byte> dst = observed.Copy();
            Image<Hsv, Byte> hsv = observed.Convert<Hsv, Byte>();
            //皮膚偵測並填為黑
            Image<Gray, Byte> skin = new Image<Gray, byte>(observed.Size);
            CvInvoke.cvInRangeS(hsv, new MCvScalar(0, 58, 89), new MCvScalar(25, 173, 229), skin);
            skin = FillHoles(skin); skin._Erode(2); skin._Dilate(2);
            Contour<Point> skinContours = DetectObjects.DoContours(skin);
            Contour<Point> max = DetectObjects.GetMaxContours(skinContours);
            //new ImageViewer(DetectObjects.DrawMaxContoursOnImg(max, dst)).Show();
            dst.Draw(max, new Bgr(0, 0, 0), -1);
            //new ImageViewer(dst).Show();
            return dst;
            
        }

        ////////////////////////////////////////////////////////////////////////////
        #endregion
        
        #region Flood Fill
		////////////////////////////////////////////////////////////////////////////
        private Image<Gray, byte> FillHoles(Image<Gray, byte> image, int minArea, int maxArea)
        {
            var resultImage = image.CopyBlank();
            Gray gray = new Gray(255);

            using (var mem = new MemStorage())
            {
                for (var contour = image.FindContours(
                    CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                    RETR_TYPE.CV_RETR_CCOMP,
                    mem); contour != null; contour = contour.HNext)
                {
                    if ((contour.Area < maxArea) && (contour.Area > minArea))
                        resultImage.Draw(contour, gray, -1);
                }
            }

            return resultImage;
        }
        private Image<Gray, byte> FillHoles(Image<Gray, byte> image)
        {
            var resultImage = image.CopyBlank();
            Gray gray = new Gray(255);
            using (var mem = new MemStorage())
            {
                for (var contour = image.FindContours(
                    CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                    RETR_TYPE.CV_RETR_CCOMP,
                    mem); contour != null; contour = contour.HNext)
                {
                    resultImage.Draw(contour, gray, -1);
                }
            }

            return resultImage;
        }
        ////////////////////////////////////////////////////////////////////////////
	    #endregion
        /// <summary>
        /// 執行辨識
        /// </summary>
        /// <param name="isDrawResultToShowOnDialog">是否要顯示出辨識的結果</param>
        /// <returns>回傳商品資訊, 格式=>"商品名稱,商品售價" ;請記得做字串切割,若無比對到或有任何問題則會回傳null</returns>
        public string RunRecognition(bool isDrawResultToShowOnDialog)
        {
            if (surfFiles.Count != 0)
            {
                //匹配特徵並取回匹配到的特徵
                KeyValuePair<String,SURFMatchedData> mathedGoodsData  = MatchRecognition.MatchSURFFeature(surfFiles, objectImg, true);
                if (mathedGoodsData.Key != null && mathedGoodsData.Value != null)
                {
                    //透過樣板檔案名稱取出匹配到的商品資訊
                    string matchedFileName = mathedGoodsData.Key;
                    //切割出商品檔案ID=> 特徵檔案名稱命名規則:(商品ID+特徵點編號),因為一種商品可能需要多張畫面的特徵點
                    string[] split = matchedFileName.Split('-');
                    string goodsMsg;
                    //特徵檔案名稱使否有存在此商品
                    if (goodsData.TryGetValue(split[0], out goodsMsg))
                    {
                        return goodsMsg;
                    }
                    else 
                    {
                        System.Windows.MessageBox.Show("特徵檔案並無存在可對應的商品資料!");
                        return null;
                    }
                }
                else 
                {
                    System.Windows.MessageBox.Show("沒有對應到的商品或是不存在此商品");
                    return null;
                }
            }
            else 
            {
                System.Windows.MessageBox.Show("沒有特徵資料");
                return null;
            }
        }
        //目前寫死,要加入的商品資訊(切記,要有對應到的特徵檔案)
        private void SetUpGoodsData() 
        {
            this.goodsData.Add("TW000000","Pocky巧克力棒,35元");
            this.goodsData.Add("TW000001","小瓜呆脆笛酥巧克力口味,34元");
            this.goodsData.Add("TW000002", "檸檬熱飲,28元");
            this.goodsData.Add("TW000003", "AB無糖優酪乳,28元");
            this.goodsData.Add("TW000004", "生活泡沫綠茶,10元");
        }
    }
}
