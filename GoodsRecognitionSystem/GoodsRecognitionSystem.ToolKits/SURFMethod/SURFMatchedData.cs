using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
//VectorOfKeyPoint
using Emgu.CV.Util;
namespace GoodsRecognitionSystem.ToolKits.SURFMethod
{
    /// <summary>
    /// 包裝了匹配到的相關訊息類別
    /// </summary>
    public class SURFMatchedData
    {
        Matrix<int> indices;
        HomographyMatrix homography = null;
        Matrix<byte> mask;
        int matchedCount;
        SURFFeatureData templateSURFData;
        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="indices"></param>
        /// <param name="homography">用來繪製ROI的矩陣</param>
        /// <param name="mask"></param>
        /// <param name="matchedCount">匹配點</param>
        /// <param name="template">樣板特徵類別</param>
        public SURFMatchedData(Matrix<int> indices, HomographyMatrix homography, Matrix<byte> mask, int matchedCount,SURFFeatureData template) 
        {
            this.indices = indices;
            this.homography = homography;
            this.mask = mask;
            this.matchedCount = matchedCount;
            this.templateSURFData = template;
        }

        public Matrix<int>  GetIndices()
        {
            return this.indices;
        }
        public HomographyMatrix GetHomography() 
        {
            return this.homography;
        }
        public Matrix<byte> GetMask() 
        {
            return this.mask;
        }
        public int GetMatchedCount() 
        {
            return this.matchedCount;
        }

        public SURFFeatureData GetTemplateSURFData()
        {
            return this.templateSURFData;
        }
    }
}
