using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//寫檔讀檔
using System.IO;
//point
using System.Drawing;
//CCHandle使用
using System.Runtime.InteropServices;
//EmguCV
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
//使用ToolKit dll
using GoodsRecognitionSystem.ToolKits;
namespace GoodsRecognitionSystem
{
    /// <summary>
    /// 偵測出可能的物體
    /// </summary>
    public static class DetectObjects
    {
        private const int DEFAULT_ERODE_INTERATIONS = 1;
        private const int DEFAULT_DILATE_INTERATIONS = 11;
       
        #region 反投影
        //////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 反投影,可選擇是標準化的輸入數值
        /// </summary>
        /// <param name="hist">輸入直方圖</param>
        /// <param name="observedSceneImg">要觀察比對的影像</param>
        /// <param name="value">標準化的參數值</param>
        /// <returns></returns>
        public static Image<Gray, Byte> DoBackProjectBeforeNormalizationHist(DenseHistogram hist, Image<Bgr, Byte> observedSceneImg, double value)
        {
            if (hist.Dimension == 1)
                return HistogramOperation.HBackProject(hist, observedSceneImg, true, value);
            else if (hist.Dimension == 2)
                return HistogramOperation.HSBackProject(hist, observedSceneImg, true, value);
            else
                return HistogramOperation.HSVBackProject(hist, observedSceneImg, true, value);
        }
        /// <summary>
        /// 反投影,會使輸入的值方圖做標準化,參數內建一維:700,二維:179*255,三維:1200000
        /// </summary>
        /// <param name="hist">輸入直方圖</param>
        /// <param name="observedSceneImg">要觀察比對的影像</param>
        /// <returns>反向投影後的灰階圖,顏色由黑接近白,代表匹配率由低到越高</returns>
        public static Image<Gray, Byte> DoBackProjectBeforeNormalizationHist(DenseHistogram hist, Image<Bgr, Byte> observedSceneImg)
        {
            if (hist.Dimension == 1)
                return HistogramOperation.HBackProject(hist, observedSceneImg, true);
            else if (hist.Dimension == 2)
                return HistogramOperation.HSBackProject(hist, observedSceneImg, true);
            else
                return HistogramOperation.HSVBackProject(hist, observedSceneImg, true);
        }
        /// <summary>
        /// 反投影,不會先做直方圖標準化
        /// </summary>
        /// <param name="hist">輸入直方圖</param>
        /// <param name="observedSceneImg">要觀察比對的影像</param>
        /// <returns>反向投影後的灰階圖,顏色由黑接近白,代表匹配率由低到越高</returns>
        public static Image<Gray, Byte> DoBackProject(DenseHistogram hist, Image<Bgr, Byte> observedSceneImg)
        {
            if (hist.Dimension == 1)
                return HistogramOperation.HBackProject(hist, observedSceneImg, false);
            else if (hist.Dimension == 2)
                return HistogramOperation.HSBackProject(hist, observedSceneImg, false);
            else
                return HistogramOperation.HSVBackProject(hist, observedSceneImg, false);
        }
        //////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        #region 讀值方圖檔
        //////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 讀取直方圖(只支援一維與二維
        /// </summary>
        /// <param name="fileName">檔案路徑名稱</param>
        /// <param name="isShowHist">是否要顯示值方圖</param>
        /// <returns>回傳值方圖類別</returns>
        public static DenseHistogram ReadHistogram(string fileName,bool isShowHist)
        {
            string format = Path.GetExtension(fileName); //取得副檔名
            DenseHistogram histogram = FeatureDataFilesOperation.ReadHistogramDataFromBinaryXml(fileName);
            Console.WriteLine("Read Histogram Data in " + format + "........\n ");
            if (isShowHist)
            {
                if (histogram.Dimension == 1) SystemToolBox.Show1DHistogramDataOnConsole(histogram);
                else if (histogram.Dimension == 2) SystemToolBox.Show2DHistogramDataOnConsole(histogram);
                
            }
            Console.WriteLine("\n");
            return histogram;
        }
        //////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        #region 二值化
        //////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 手動輸入閥值與來源影像
        /// </summary>
        /// <param name="srcImg">輸入來源</param>
        /// <param name="thersValue">二值化的值</param>
        /// <returns>二值化影像</returns>
        public static Image<Gray, Byte> DoBinaryThreshold(Image<Gray, Byte> srcImg, int thersValue)
        {
            Image<Gray, Byte> binaryImg = srcImg.Copy();

            return binaryImg.ThresholdBinary(new Gray(thersValue), new Gray(255));
        }

        /// <summary>
        /// 使用Otsu機率分布取得閥值並二值化
        /// </summary>
        /// <param name="srcImg">輸入來源</param>
        /// <returns>二值化影像</returns>
        public static Image<Gray, Byte> DoBinaryThresholdByOtsu(Image<Gray, Byte> srcImg)
        {
            Image<Gray, Byte> binaryImg = srcImg.Copy();
            CvInvoke.cvThreshold(binaryImg, binaryImg, 160d, 255d, THRESH.CV_THRESH_OTSU); //使用otsu時,參數三還是要填不然會錯誤(但不會使用)
            return binaryImg;
        }
        //////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        #region 侵蝕膨脹
        //////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 對二值化後的影像侵蝕,可選擇要先侵蝕還是膨脹或顛倒
        /// </summary>
        /// <param name="srcImg">來源的二值化過後影像</param>
        /// <param name="iteration">要侵蝕的次數(侵蝕會使用3x3的矩形)</param>
        /// <returns></returns>
        public static Image<Gray, Byte> DoErode(Image<Gray, Byte> srcImg, int iteration)
        {
            Image<Gray, Byte> erodeImg = srcImg.Copy();
            return erodeImg.Erode(iteration);
        }
        /// <summary>
        /// 對二值化後的影像侵蝕,可選擇要先侵蝕還是膨脹或顛倒,預設侵蝕次數為1
        /// </summary>
        /// <param name="srcImg">來源的二值化過後影像</param>
        /// <returns></returns>
        public static Image<Gray, Byte> DoErode(Image<Gray, Byte> srcImg)
        {
            Image<Gray, Byte> erodeImg = srcImg.Copy();
            return erodeImg.Erode(DEFAULT_ERODE_INTERATIONS);
        }

        /// <summary>
        /// 對二值化後的影像膨脹,可選擇要先侵蝕還是膨脹或顛倒
        /// </summary>
        /// <param name="srcImg">來源的二值化過後影像</param>
        /// <param name="iteration">要膨脹的次數(膨脹的會使用3x3的矩形)</param>
        /// <returns></returns>
        public static Image<Gray, Byte> DoDilate(Image<Gray, Byte> srcImg, int iteration)
        {
            Image<Gray, Byte> dilateImg = srcImg.Copy();
            return dilateImg.Dilate(iteration);
        }

        /// <summary>
        /// 對二值化後的影像膨脹,可選擇要先侵蝕還是膨脹或顛倒,預設膨脹次數是11
        /// </summary>
        /// <param name="srcImg">來源的二值化過後影像</param>
        /// <returns></returns>
        public static Image<Gray, Byte> DoDilate(Image<Gray, Byte> srcImg)
        {
            Image<Gray, Byte> dilateImg = srcImg.Copy();
            return dilateImg.Dilate(DEFAULT_DILATE_INTERATIONS);
        }
        //////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        #region 取輪廓
        //////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 從圖像中取得所有的輪廓
        /// </summary>
        /// <param name="srcImg">來源圖像,這邊是二值化侵蝕膨脹後的圖像</param>
        /// <returns>回傳輪廓</returns>
        public static Contour<Point> DoContours(Image<Gray, Byte> srcImg)
        {
            Contour<Point> objectContours = srcImg.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST);
            //Contour<Point> objectContours = srcImg.FindContours();
            return objectContours;
        }
        /// <summary>
        /// 取得輪廓資料中的最大輪廓,若要取得最大輪廓的BoundingBox,使用contours.BoundingBox
        /// </summary>
        /// <param name="contours">輸入從圖像中取得的所有輪廓</param>
        /// <returns>回傳最大面積的輪廓</returns>
        public static Contour<Point> GetMaxContours(Contour<Point> contours)
        {
            Contour<Point> MaxContour = contours;
            while (contours.HNext != null)
            {
                if (MaxContour.Area < contours.Area)
                {
                    MaxContour = contours;
                }
                contours = contours.HNext;
            }
            return MaxContour;
        }
        /// <summary>
        /// 從影像上取得輪廓的BoundingBox(影像ROI)
        /// </summary>
        /// <param name="contours">輪廓類別</param>
        /// <param name="sceneImg">場景影像</param>
        /// <returns>回傳ROI影像</returns>
        public static Image<Bgr, Byte> GetBoundingBoxImage(Contour<Point> contours, Image<Bgr, Byte> sceneImg)
        {
            Image<Bgr, Byte> roiImage = sceneImg.GetSubRect(contours.BoundingRectangle);
            return roiImage;
        }
        /// <summary>
        /// 劃出所有輪廓到圖像上
        /// </summary>
        /// <param name="contours">取得的輪廓</param>
        /// <param name="drawImg">要畫到的圖像上</param>
        /// <returns>回傳畫上輪廓的圖像</returns>
        public static Image<Bgr, Byte> DrawAllContoursOnImg(Contour<Point> contours, Image<Bgr, Byte> drawImg)
        {
            drawImg.Draw(contours, new Bgr(Color.Red), new Bgr(Color.Yellow), 1, 2);
            return drawImg;
        }
        /// <summary>
        /// 畫上最大的輪廓到圖像上
        /// </summary>
        /// <param name="maxContour">最大的輪廓</param>
        /// <param name="drawImg">要畫到的圖像上</param>
        /// <returns>回傳畫上輪廓的圖像</returns>
        public static Image<Bgr, Byte> DrawMaxContoursOnImg(Contour<Point> maxContour, Image<Bgr, Byte> drawImg)
        {
            drawImg.Draw(maxContour, new Bgr(Color.Red), 2);
            return drawImg;
        }
        /// <summary>
        /// 畫上最大輪廓的BoundimgBox
        /// </summary>
        /// <param name="maxContour">最大的輪廓</param>
        /// <param name="drawImg">要畫到的圖像上</param>
        /// <returns>回傳畫上最大輪廓的BoundingBox的圖像</returns>
        public static Image<Bgr, Byte> DrawContoursMaxBoundingBoxOnImg(Contour<Point> maxContour, Image<Bgr, Byte> drawImg)
        {
            drawImg.Draw(maxContour.BoundingRectangle, new Bgr(Color.Red), 2);
            return drawImg;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        #region 匹配值方圖
        //////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 直方圖匹配(使用BHATTACHARYYA)
        /// </summary>
        /// <param name="template">樣板值方圖</param>
        /// <param name="observedSrcImg">要比對的圖像</param>
        /// <returns>匹配率越低表示匹配度越高</returns>
        public static double CompareHistogram(DenseHistogram template, Image<Bgr, Byte> observedSrcImg)
        {
            DenseHistogram observedHist;
            //計算影像的值方圖
            if (template.Dimension == 1)
                observedHist = HistogramOperation.CalHsvHistogram(observedSrcImg.Ptr, template.Dimension, template.BinDimension[0].Size);
            else if (template.Dimension == 2)
                observedHist = HistogramOperation.CalHsvHistogram(observedSrcImg.Ptr, template.Dimension, template.BinDimension[0].Size, template.BinDimension[1].Size);
            else
                observedHist = HistogramOperation.CalHsvHistogram(observedSrcImg.Ptr, template.Dimension, template.BinDimension[0].Size, template.BinDimension[1].Size, template.BinDimension[2].Size);
            //匹配後回傳匹配率
            return HistogramOperation.CompareHist(template, observedHist);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
    }
}
