using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//Marshal類別
using System.Runtime.InteropServices;
//EmguCV
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
namespace GoodsRecognitionSystem.ToolKits
{
    /// <summary>
    /// Emgucv與Emgucv中的openCV格式轉換
    /// </summary>
    public static class EmguFormatConvetor
    {
        #region emgucv與openCV之間的圖像格式轉換
        /// <summary>
        /// MIplImage是IplImage中的託管實現，它是.net與OpenCv之間溝通的橋樑
        /// 將IplImage指標轉換成MIplImage結構
        /// </summary>
        /// <param name="ptr">IplImage指標</param>
        /// <returns></returns>
        private static MIplImage IplImagePointerToMIplImage(IntPtr ptr)
        {
            return (MIplImage)Marshal.PtrToStructure(ptr, typeof(MIplImage));
        }

        /// <summary>
        /// 將IplImage指針轉換成Emgucv中的Image對象；
        /// 注意：這裡需要您自己根據IplImage中的depth和nChannels來決定
        /// </summary>
        /// <typeparam  name = "TColor" >Color type of this image (either Gray, Bgr, Bgra, Hsv, Hls, Lab, Luv, Xyz or Ycc)</typeparam>
        /// <typeparam  name = "TDepth" >Depth of this image (either Byte, SByte, Single, double, UInt16, Int16 or Int32)</typeparam>
        /// <param  name = "ptr" >IplImage指針</param>
        /// <returns >返回Image對象</returns>
        public static Image<TColor, TDepth> IplImagePointerToEmgucvImage<TColor, TDepth>(IntPtr ptr)
            where TColor : struct, IColor
            where TDepth : new()
        {
            MIplImage mi = IplImagePointerToMIplImage(ptr);
            return new Image<TColor, TDepth>(mi.width, mi.height, mi.widthStep, mi.imageData);
        }

        #endregion
    }
}
