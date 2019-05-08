using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
//CCHandle使用
using System.Runtime.InteropServices;
//EmguCV
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
namespace GoodsRecognitionSystem.ToolKits
{
    /// <summary>
    /// 值方圖相關運算
    /// </summary>
    public class HistogramOperation
    {

        const float h_max_range = 180;
        const float s_max_range = 255;
        const float v_max_range = 255;

        #region 計算值方圖
        //////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 計算HSV值方圖
        /// </summary>
        /// <param name="srcPtr">指標模式的圖片源</param>
        /// <param name="dim">選取維度計算,1 = 只取hue,2 = H-S,3 = H-S-V,預設為1</param>
        /// <param name="h_bins">H色調預設50,2維建議改30</param>
        /// <param name="s_bins">S飽和度預設0,2維建議改32</param>
        /// <param name="v_bins">S飽和度預設0,</param>
        /// <returns>回傳EmguCV的DenseHistogram型態,若要繪製與寫檔讀檔皆使用此類別</returns>
        public static DenseHistogram CalHsvHistogram(IntPtr srcPtr, int dim = 1, int h_bins = 50, int s_bins = 0, int v_bins = 0)
        {
            IntPtr srcImage = srcPtr;
            if (dim == 1)
                return Cal1DHsvHist(srcImage, h_bins);
            else if (dim == 2)
                return Cal2DHsvHist(srcImage, h_bins, s_bins);
            else
                return Cal3DHsvHist(srcImage, h_bins, s_bins, v_bins);
        }
        /// <summary>
        /// 計算HSV值方圖(色調與飽和度或純色調)
        /// </summary>
        /// <param name="srcImg">讀取全彩圖片源</param>
        /// <param name="dim">選取維度計算,1 = 只取hue,2 = H-S,3 = H-S-V,預設為1</param>
        /// <param name="h_bins">H色調預設50,2維建議改30</param>
        /// <param name="s_bins">S飽和度預設0,2維建議改32</param>
        /// <param name="v_bins">S飽和度預設0,</param>
        /// <returns>回傳EmguCV的DenseHistogram型態,若要繪製與寫檔讀檔皆使用此類別</returns>
        public static DenseHistogram CalHsvHistogram(Image<Bgr, Byte> srcImg, int dim = 1, int h_bins = 50, int s_bins = 0, int v_bins = 0)
        {
            IntPtr srcImage = srcImg.Copy().Ptr; //ok
            //CvInvoke.cvCopy(srcImage, srcImg.Ptr, IntPtr.Zero);
            if (dim == 1)
                return Cal1DHsvHist(srcImage, h_bins);
            else if (dim == 2)
                return Cal2DHsvHist(srcImage, h_bins, s_bins);
            else
                return Cal3DHsvHist(srcImage, h_bins, s_bins, v_bins);
        }
        /// <summary>
        /// 計算HSV值方圖(色調與飽和度)
        /// </summary>
        /// <param name="srcFileName">填入檔案路徑</param>
        /// <param name="dim">選取維度計算,1 = 只取hue,2 = H-S,3 = H-S-V,預設為1</param>
        /// <param name="h_bins">H色調預設50,2維建議改30</param>
        /// <param name="s_bins">S飽和度預設0,2維建議改32</param>
        /// <param name="v_bins">S飽和度預設0,</param>
        /// <returns>回傳EmguCV的DenseHistogram型態,若要做繪製(GenerateHistogramImgForDraw())與寫檔讀檔皆使用此類別</returns>
        public static DenseHistogram CalHsvHistogram(string srcFileName, int dim = 1, int h_bins = 50, int s_bins = 0, int v_bins = 0)
        {
            IntPtr srcImage = CvInvoke.cvLoadImage(srcFileName, Emgu.CV.CvEnum.LOAD_IMAGE_TYPE.CV_LOAD_IMAGE_ANYCOLOR);
            if (dim == 1)
                return Cal1DHsvHist(srcImage, h_bins);
            else if (dim == 2)
                return Cal2DHsvHist(srcImage, h_bins, s_bins);
            else
                return Cal3DHsvHist(srcImage, h_bins, s_bins, v_bins);
        }


        private static DenseHistogram Cal3DHsvHist(IntPtr srcImage, int h_bins, int s_bins, int v_bins)
        {
            try
            {
                DenseHistogram histDense;
                int[] hist_size = new int[3] { h_bins, s_bins, v_bins };
                IntPtr hsv = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(srcImage), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
                IntPtr h_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(srcImage), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                IntPtr s_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(srcImage), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                IntPtr v_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(srcImage), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                IntPtr[] planes = new IntPtr[3] { h_plane, s_plane, v_plane };

                /* H 分量的变化范围 */
                float[] h_ranges = new float[2] { 0, h_max_range };

                /* S 分量的变化范围*/
                float[] s_ranges = new float[2] { 0, s_max_range };

                /* V 分量的变化范围*/
                float[] v_ranges = new float[2] { 0, v_max_range };
                IntPtr inPtr1 = new IntPtr(0);
                IntPtr inPtr2 = new IntPtr(0);
                IntPtr inPtr3 = new IntPtr(0);
                //GCHandle:提供從Unmanaged 記憶體存取Managed 物件的方法。
                //配置指定型別的數值記憶體
                GCHandle gch1 = GCHandle.Alloc(h_ranges, GCHandleType.Pinned);
                GCHandle gch2 = GCHandle.Alloc(s_ranges, GCHandleType.Pinned);
                GCHandle gch3 = GCHandle.Alloc(v_ranges, GCHandleType.Pinned);
                try
                {
                    inPtr1 = gch1.AddrOfPinnedObject();
                    inPtr2 = gch2.AddrOfPinnedObject();
                    inPtr3 = gch3.AddrOfPinnedObject();
                }
                finally
                {
                    gch1.Free();
                    gch2.Free();
                    gch3.Free();
                }
                //有上述的GCHandle,此行才有作用
                IntPtr[] ranges = new IntPtr[3] { inPtr1, inPtr2, inPtr3 };

                /* 输入图像转换到HSV颜色空间 */
                CvInvoke.cvCvtColor(srcImage, hsv, Emgu.CV.CvEnum.COLOR_CONVERSION.CV_BGR2HSV);
                CvInvoke.cvSplit(hsv, h_plane, s_plane, v_plane, System.IntPtr.Zero); // 分离的单通道数组d
                /* 创建直方图，二维, 每个维度上均分 */
                //emgucv的DenseHistogram資料格式也可使用cvInvoke的openCV函式
                RangeF hRange = new RangeF(0f, h_max_range);       //H色調分量的變化範圍
                RangeF sRange = new RangeF(0f, s_max_range);       //S飽和度分量的變化範圍
                RangeF vRange = new RangeF(0f, v_max_range);       //V分量的變化範圍
                histDense = new DenseHistogram(hist_size, new RangeF[] { hRange, sRange, vRange });
                CvInvoke.cvCalcHist(planes, histDense, false, System.IntPtr.Zero);
                return histDense;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
        /// <summary>
        /// 2D計算值方圖(色調與飽和度) ,使用emgucv提供的cvInvoke去調用opencv的函式
        /// </summary>
        private static DenseHistogram Cal2DHsvHist(IntPtr srcImage, int h_bins, int s_bins)
        {
            try
            {
                DenseHistogram histDense;
                int[] hist_size = new int[2] { h_bins, s_bins };
                IntPtr hsv = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(srcImage), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
                IntPtr h_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(srcImage), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                IntPtr s_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(srcImage), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                IntPtr v_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(srcImage), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                IntPtr[] planes = new IntPtr[2] { h_plane, s_plane };

                /* H 分量的变化范围 */
                float[] h_ranges = new float[2] { 0, h_max_range };

                /* S 分量的变化范围*/
                float[] s_ranges = new float[2] { 0, s_max_range };
                IntPtr inPtr1 = new IntPtr(0);
                IntPtr inPtr2 = new IntPtr(0);

                //GCHandle:提供從Unmanaged 記憶體存取Managed 物件的方法。
                //配置指定型別的數值記憶體
                GCHandle gch1 = GCHandle.Alloc(h_ranges, GCHandleType.Pinned);
                GCHandle gch2 = GCHandle.Alloc(s_ranges, GCHandleType.Pinned);
                try
                {
                    inPtr1 = gch1.AddrOfPinnedObject();
                    inPtr2 = gch2.AddrOfPinnedObject();
                }
                finally
                {
                    gch1.Free();
                    gch2.Free();
                }
                //有上述的GCHandle,此行才有作用
                IntPtr[] ranges = new IntPtr[2] { inPtr1, inPtr2 };

                /* 输入图像转换到HSV颜色空间 */
                CvInvoke.cvCvtColor(srcImage, hsv, Emgu.CV.CvEnum.COLOR_CONVERSION.CV_BGR2HSV);
                CvInvoke.cvSplit(hsv, h_plane, s_plane, v_plane, System.IntPtr.Zero); // 分离的单通道数组d

                /* 创建直方图，二维, 每个维度上均分 */
                //emgucv的DenseHistogram資料格式也可使用cvInvoke的openCV函式
                RangeF hRange = new RangeF(0f, h_max_range);       //H色調分量的變化範圍
                RangeF sRange = new RangeF(0f, s_max_range);       //S飽和度分量的變化範圍
                histDense = new DenseHistogram(hist_size, new RangeF[] { hRange, sRange });
                CvInvoke.cvCalcHist(planes, histDense, false, System.IntPtr.Zero);
                return histDense;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        /// <summary>
        /// 1D計算值方圖(色調與飽和度) ,使用emgucv提供的cvInvoke去調用opencv的函式 ok
        /// </summary>
        private static DenseHistogram Cal1DHsvHist(IntPtr srcImage, int h_bins)
        {
            try
            {
                DenseHistogram histDense;
                IntPtr hsv = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(srcImage), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
                IntPtr h_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(srcImage), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);

                /* H 分量的变化范围 */
                float[] h_ranges = new float[2] { 0, h_max_range };
                IntPtr[] h_plane_ptr = new IntPtr[] { h_plane };
                IntPtr inPtr1 = new IntPtr(0);
                //GCHandle:提供從Unmanaged 記憶體存取Managed 物件的方法。
                //配置指定型別的數值記憶體
                GCHandle gch1 = GCHandle.Alloc(h_ranges, GCHandleType.Pinned);
                try
                {
                    inPtr1 = gch1.AddrOfPinnedObject();
                }
                finally
                {
                    gch1.Free();
                }
                //有上述的GCHandle,此行才有作用
                IntPtr[] ranges = new IntPtr[] { inPtr1 };

                /* 输入图像转换到HSV颜色空间 */
                CvInvoke.cvCvtColor(srcImage, hsv, Emgu.CV.CvEnum.COLOR_CONVERSION.CV_BGR2HSV);
                CvInvoke.cvSplit(hsv, h_plane, IntPtr.Zero, IntPtr.Zero, System.IntPtr.Zero); // 分离的单通道数组d

                /* 创建直方图，一维, 每个维度上均分 */
                //emgucv的DenseHistogram資料格式也可使用cvInvoke的openCV函式
                RangeF hRange = new RangeF(0f, h_max_range);       //H色調分量的變化範圍
                histDense = new DenseHistogram(h_bins, hRange);
                CvInvoke.cvCalcHist(h_plane_ptr, histDense, true, System.IntPtr.Zero);

                return histDense;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        /// <summary>
        /// hue色相轉換成rgb color
        /// </summary>
        /// <param name="hue"></param>
        /// <returns></returns>
        private static MCvScalar HueToBgr(double hue)
        {
            int[] rgb = new int[3];
            int p, sector;
            int[,] sector_data = { { 0, 2, 1 }, { 1, 2, 0 }, { 1, 0, 2 }, { 2, 0, 1 }, { 2, 1, 0 }, { 0, 1, 2 } };
            hue *= 0.033333333333333333333333333333333f;
            sector = (int)Math.Floor(hue);
            p = (int)Math.Round(255 * (hue - sector));
            //p ^= sector & 1 ? 255 : 0;
            if ((sector & 1) == 1) p ^= 255;
            else p ^= 0;
            rgb[sector_data[sector, 0]] = 255;
            rgb[sector_data[sector, 1]] = 0;
            rgb[sector_data[sector, 2]] = p;
            MCvScalar scalar = new MCvScalar(rgb[2], rgb[1], rgb[0], 0);
            return scalar;
        }

        #region 繪製值方圖
        //////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// drawing hue color still have problem
        /// 1D值方圖(色調) 的繪製,使用emgucv提供的cvInvoke去調用opencv的函式
        /// 繪製與範例的值方圖一致目前先採用
        /// </summary>
        /// <param name="histDense"></param>
        /// <returns>回傳繪製值方圖的影像,直接顯示即可</returns>
        public static Image<Bgr, Byte> Generate1DHistogramImgForDraw(DenseHistogram histDense)
        {
            try
            {
                float max_value = 0.0f;
                int[] a1 = new int[100];
                int[] b1 = new int[100];
                float ax = 0;
                int h_bins = histDense.BinDimension[0].Size;

                //1.使用Intptr
                // CvInvoke.cvGetMinMaxHistValue(histPtr, ref ax, ref max_value, a1, b1);

                //2.emgucv的DenseHistogram資料格式也可使用cvInvoke的openCV函式
                CvInvoke.cvGetMinMaxHistValue(histDense, ref ax, ref max_value, a1, b1);
                /* 取最大的顏色的位置 並換成RGB
                foreach (int index in a1)
                {
                    Console.WriteLine("location="+index+",H Color = "+ HueToBgr(index * 180.0d / h_bins));
                } 
                 * */
                /* 设置直方图显示图像 */
                int height = 240;
                int width = 800;
                IntPtr hist_img = CvInvoke.cvCreateImage(new System.Drawing.Size(width, height), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
                CvInvoke.cvZero(hist_img);

                /* 用来进行HSV到RGB颜色转换的临时单位图像 */
                IntPtr hsv_color = CvInvoke.cvCreateImage(new System.Drawing.Size(1, 1), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
                IntPtr rgb_color = CvInvoke.cvCreateImage(new System.Drawing.Size(1, 1), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
                int bin_w = width / (h_bins);

                for (int h = 0; h < h_bins; h++)
                {

                    /* 获得直方图中的统计次数，计算显示在图像中的高度 */
                    //
                    //取得值方圖的數值位置,以便之後存成檔案
                    //2.DenseHistogram
                    double bin_val = CvInvoke.cvQueryHistValue_1D(histDense, h);
                    int intensity = (int)System.Math.Round(bin_val * height / max_value);

                    /* 获得当前直方图代表的hue颜色，转换成RGB用于绘制 */
                    CvInvoke.cvRectangle(hist_img, new System.Drawing.Point(h * bin_w, height),
                        new System.Drawing.Point((h + 1) * bin_w, height - intensity),
                        HueToBgr(h * 180.0d / h_bins), -1, Emgu.CV.CvEnum.LINE_TYPE.EIGHT_CONNECTED, 0);

                }

                /*
                 *使用openCV函式繪製
                CvInvoke.cvNamedWindow("Source");
                CvInvoke.cvShowImage("Source", this.srcImage);
                CvInvoke.cvNamedWindow("H-S Histogram");
                CvInvoke.cvShowImage("H-S Histogram", hist_img);
                CvInvoke.cvWaitKey(0);
                 * */
                return EmguFormatConvetor.IplImagePointerToEmgucvImage<Bgr, Byte>(hist_img);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        /// <summary>
        /// 2D值方圖(色調與飽和度) 的繪製,使用emgucv提供的cvInvoke去調用opencv的函式
        /// 繪製與範例的值方圖一致目前先採用
        /// </summary>
        /// <param name="histDense"></param>
        /// <returns>回傳繪製值方圖的影像,直接顯示即可</returns>
        public static Image<Bgr, Byte> Generate2DHistogramImgForDraw(DenseHistogram histDense)
        {
            try
            {
                float max_value = 0.0f;
                int[] a1 = new int[100];
                int[] b1 = new int[100];
                float ax = 0;
                int h_bins = histDense.BinDimension[0].Size;
                int s_bins = histDense.BinDimension[1].Size;

                //1.使用Intptr
                // CvInvoke.cvGetMinMaxHistValue(histPtr, ref ax, ref max_value, a1, b1);

                //2.emgucv的DenseHistogram資料格式也可使用cvInvoke的openCV函式
                CvInvoke.cvGetMinMaxHistValue(histDense, ref ax, ref max_value, a1, b1);

                /* 设置直方图显示图像 */
                int height = 300;
                int width;
                //如果設定的bins超過視窗設定的顯示範圍,另外給予可以符合用額外的彈出視窗顯示的值
                if (h_bins * s_bins > 800)
                {
                    width = h_bins * s_bins * 2;
                }
                else
                {
                    width = 800;
                }

                IntPtr hist_img = CvInvoke.cvCreateImage(new System.Drawing.Size(width, height), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
                CvInvoke.cvZero(hist_img);

                /* 用来进行HSV到RGB颜色转换的临时单位图像 */
                IntPtr hsv_color = CvInvoke.cvCreateImage(new System.Drawing.Size(1, 1), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
                IntPtr rgb_color = CvInvoke.cvCreateImage(new System.Drawing.Size(1, 1), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
                int bin_w = width / (h_bins * s_bins);

                for (int h = 0; h < h_bins; h++)
                {
                    for (int s = 0; s < s_bins; s++)
                    {
                        int i = h * s_bins + s;
                        /* 获得直方图中的统计次数，计算显示在图像中的高度 */
                        //
                        //取得值方圖的數值位置,以便之後存成檔案
                        //1.Intptr
                        //double bin_val = CvInvoke.cvQueryHistValue_2D(histPtr, h, s);

                        //2.DenseHistogram
                        double bin_val = CvInvoke.cvQueryHistValue_2D(histDense, h, s);
                        int intensity = (int)System.Math.Round(bin_val * height / max_value);

                        /* 获得当前直方图代表的颜色，转换成RGB用于绘制 */
                        CvInvoke.cvSet2D(hsv_color, 0, 0, new Emgu.CV.Structure.MCvScalar(h * 180.0f / h_bins, s * 255.0f / s_bins, 255, 0));
                        CvInvoke.cvCvtColor(hsv_color, rgb_color, COLOR_CONVERSION.CV_HSV2BGR);
                        Emgu.CV.Structure.MCvScalar color = CvInvoke.cvGet2D(rgb_color, 0, 0);
                        CvInvoke.cvRectangle(hist_img, new System.Drawing.Point(i * bin_w, height), new System.Drawing.Point((i + 1) * bin_w, height - intensity), color, -1, Emgu.CV.CvEnum.LINE_TYPE.EIGHT_CONNECTED, 0);
                    }
                }

                /*
                 *使用openCV函式繪製
                CvInvoke.cvNamedWindow("Source");
                CvInvoke.cvShowImage("Source", this.srcImage);
                CvInvoke.cvNamedWindow("H-S Histogram");
                CvInvoke.cvShowImage("H-S Histogram", hist_img);
                CvInvoke.cvWaitKey(0);
                 * */
                return EmguFormatConvetor.IplImagePointerToEmgucvImage<Bgr, Byte>(hist_img);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        #region 反投影
        //////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 反向投影(1維 Hue),需搭配1維的直方圖計算
        /// </summary>
        /// <param name="template">樣板值方圖</param>
        /// <param name="observedSrcImg">要比對觀察的影像</param>
        /// <param name="isNormalized">是否要標準化</param>
        /// <param name="factor">標準化參數,預設700d</param>
        /// <returns>回除灰階的觀察影像(越白代表匹配的顏色越高)</returns>
        public static Image<Gray, Byte> HBackProject(DenseHistogram template, Image<Bgr, Byte> observedSrcImg, bool isNormalized = true, double factor = 700d)
        {
            try
            {
                DenseHistogram templateHist = new DenseHistogram(template.BinDimension[0].Size, new RangeF(0, h_max_range));
                template.Copy(templateHist);
                Image<Bgr, Byte> observedImg = observedSrcImg.Copy();
                IntPtr hsv = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(observedImg), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
                IntPtr h_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(observedImg), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);

                CvInvoke.cvCvtColor(observedImg, hsv, Emgu.CV.CvEnum.COLOR_CONVERSION.CV_BGR2HSV);
                CvInvoke.cvSplit(hsv, h_plane, IntPtr.Zero, IntPtr.Zero, System.IntPtr.Zero); // 分离的单通道数组d
                if (isNormalized)
                    templateHist.Normalize(factor);

                IntPtr backProj = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(h_plane), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                CvInvoke.cvZero(backProj);
                CvInvoke.cvCalcBackProject(new IntPtr[] { h_plane }, backProj, templateHist);

                return EmguFormatConvetor.IplImagePointerToEmgucvImage<Gray, Byte>(backProj);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        /// <summary>
        /// 反向投影(2維 H-S),需搭配2維的直方圖計算
        /// </summary>
        /// <param name="template">樣板值方圖</param>
        /// <param name="observedSrcImg">要比對觀察的影像</param>
        /// <param name="isNormalized">是否要標準化</param>
        /// <param name="factor">標準化參數,預設179*255d</param>
        /// <returns>回除灰階的觀察影像(越白代表匹配的顏色越高)</returns>
        public static Image<Gray, Byte> HSBackProject(DenseHistogram template, Image<Bgr, Byte> observedSrcImg, bool isNormalized = true, double factor = 179*255d)
        {
            try
            {
                DenseHistogram templateHist = new DenseHistogram(new int[] { template.BinDimension[0].Size, template.BinDimension[1].Size }, new RangeF[] { new RangeF(0, h_max_range), new RangeF(0, s_max_range) });
                template.Copy(templateHist);
                Image<Bgr, Byte> observedImg = observedSrcImg.Copy();
                IntPtr hsv = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(observedImg), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
                IntPtr h_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(observedImg), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                IntPtr s_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(observedImg), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                IntPtr v_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(observedImg), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                IntPtr[] observed_planes = new IntPtr[2] { h_plane, s_plane };
                //Image<Gray, Byte> h_planeImg = IplImagePointerToEmgucvImage<Gray, Byte>(h_plane);
                //Image<Gray, Byte> s_planeImg = IplImagePointerToEmgucvImage<Gray, Byte>(s_plane);
                CvInvoke.cvCvtColor(observedImg, hsv, Emgu.CV.CvEnum.COLOR_CONVERSION.CV_BGR2HSV);
                CvInvoke.cvSplit(hsv, h_plane, s_plane, IntPtr.Zero, System.IntPtr.Zero); // 分离的单通道数组d
                if (isNormalized)
                    templateHist.Normalize(factor);

                IntPtr backProj = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(observedImg), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                CvInvoke.cvZero(backProj);
                CvInvoke.cvCalcBackProject(observed_planes, backProj, templateHist);

                return EmguFormatConvetor.IplImagePointerToEmgucvImage<Gray, Byte>(backProj);

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }



        /// <summary>
        /// 反向投影(3維),需搭配3維的直方圖計算
        /// </summary>
        /// <param name="template">樣板值方圖</param>
        /// <param name="observedSrcImg">要比對觀察的影像</param>
        /// <param name="isNormalized">是否要標準化</param>
        /// <param name="factor">標準化參數,預設1200000d</param>
        /// <returns>回除灰階的觀察影像(越白代表匹配的顏色越高)</returns>
        public static Image<Gray, Byte> HSVBackProject(DenseHistogram template, Image<Bgr, Byte> observedSrcImg, bool isNormalized = true, double factor = 1200000d)
        {
            try
            {
                DenseHistogram templateHist = new DenseHistogram(new int[] { template.BinDimension[0].Size, template.BinDimension[1].Size, template.BinDimension[2].Size }, new RangeF[] { new RangeF(0, h_max_range), new RangeF(0, s_max_range), new RangeF(0, v_max_range) });
                template.Copy(templateHist);
                Image<Bgr, Byte> observedImg = observedSrcImg.Copy();
                IntPtr hsv = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(observedImg), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
                IntPtr h_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(observedImg), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                IntPtr s_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(observedImg), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                IntPtr v_plane = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(observedImg), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                IntPtr[] observed_planes = new IntPtr[3] { h_plane, s_plane, v_plane };
                //Image<Gray, Byte> h_planeImg = IplImagePointerToEmgucvImage<Gray, Byte>(h_plane);
                //Image<Gray, Byte> s_planeImg = IplImagePointerToEmgucvImage<Gray, Byte>(s_plane);
                CvInvoke.cvCvtColor(observedImg, hsv, Emgu.CV.CvEnum.COLOR_CONVERSION.CV_BGR2HSV);
                CvInvoke.cvSplit(hsv, h_plane, s_plane, v_plane, System.IntPtr.Zero); // 分离的单通道数组d
                if (isNormalized)
                    templateHist.Normalize(factor);

                IntPtr backProj = CvInvoke.cvCreateImage(CvInvoke.cvGetSize(observedImg), Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
                CvInvoke.cvZero(backProj);
                CvInvoke.cvCalcBackProject(observed_planes, backProj, templateHist);

                return EmguFormatConvetor.IplImagePointerToEmgucvImage<Gray, Byte>(backProj);

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
        //////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        #region 直方圖匹配
        //////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 匹配值方圖,使用CV_COMP_BHATTACHARYYA
        /// </summary>
        /// <param name="templateHist"></param>
        /// <param name="observedHist"></param>
        /// <returns>回傳匹配率,CV_COMP_BHATTACHARYYA方法,數值越低比對越精準,反之相似度低,範圍0-1</returns>
        public static double CompareHist(DenseHistogram templateHist, DenseHistogram observedHist)
        {
            return CvInvoke.cvCompareHist(templateHist, observedHist, HISTOGRAM_COMP_METHOD.CV_COMP_BHATTACHARYYA);
        }
        //////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
    }
}
