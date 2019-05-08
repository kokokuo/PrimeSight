using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//stopWatch計算時間用
using System.Diagnostics;
//PointF
using System.Drawing;
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
    /// SURF運算相關類別,可用來計算特徵或匹配特徵
    /// </summary>
    public class SURFMatch
    {
        /// <summary>
        /// 計算特徵點
        /// </summary>
        /// <param name="srcImage">來源影像</param>
        /// <param name="surfParam">SURF的參數</param>
        /// <returns>回傳特徵類別</returns>
        public static SURFFeatureData CalSURFFeature(Image<Bgr, Byte> srcImage, MCvSURFParams surfParam)
        {
            SURFDetector surfCPU = new SURFDetector(surfParam);
            VectorOfKeyPoint keyPoints;
            Matrix<float> descriptors = null;

            using (Image<Gray, Byte> grayImg = srcImage.Convert<Gray, Byte>())
            {
                keyPoints = surfCPU.DetectKeyPointsRaw(grayImg, null);
                descriptors = surfCPU.ComputeDescriptorsRaw(grayImg, null, keyPoints);

            }
            return new SURFFeatureData(srcImage.Copy(), keyPoints, descriptors);
        }
        /// <summary>
        /// 計算特徵點
        /// </summary>
        /// <param name="srcImage">來源影像</param>
        /// <returns>回傳特徵類別</returns>
        public static SURFFeatureData CalSURFFeature(Image<Bgr, Byte> srcImage)
        {
            SURFDetector surfCPU = new SURFDetector(new MCvSURFParams(1200, false)); //預設500
            VectorOfKeyPoint keyPoints;
            Matrix<float> descriptors = null;
            using (Image<Gray, Byte> grayImg = srcImage.Convert<Gray, Byte>())
            {
                keyPoints = surfCPU.DetectKeyPointsRaw(grayImg, null);
                descriptors = surfCPU.ComputeDescriptorsRaw(grayImg, null, keyPoints);
            }
            return new SURFFeatureData(srcImage.Copy(), keyPoints, descriptors);
        }

        #region Features2DTracker use but have some problem
        /*
        public static int MatchSURFFeatureByFLANN2(SURFFeatureData template, SURFFeatureData observedScene, bool isDraw)
        {
            List<KeyValuePair<int, int>> ptPairs = new List<KeyValuePair<int, int>>();
            ImageFeature<float>[] modelFeature = ImageFeature<float>.ConvertFromRaw(template.GetKeyPoints(), template.GetDescriptors());
            ImageFeature<float>[] observedFeature = ImageFeature<float>.ConvertFromRaw(observedScene.GetKeyPoints(), observedScene.GetDescriptors());
            int goodMatchCount = 0;
            Stopwatch watch;
            watch = Stopwatch.StartNew();
            goodMatchCount = FlannFindPairs(modelFeature, observedFeature, ref ptPairs);
            watch.Stop();
            Console.WriteLine("\nCal SURF Match time=======\n=> " + watch.ElapsedMilliseconds.ToString() + "ms\nCal SURF Match time=======");
            return goodMatchCount;
        }
        */
        #endregion

        //FLANN匹配驗算法
        private static int FlannFindPairs(ImageFeature<float>[] modelDescriptors, ImageFeature<float>[] imageDescriptors, ref List<KeyValuePair<int, int>> ptPairs)
        {
            //Check if we have some valid model descriptors
            if (modelDescriptors.Length == 0)
                return -1;

            int length = modelDescriptors[0].Descriptor.Length;

            //Create matrix object and matrix image
            var matrixModel = new Matrix<float>(modelDescriptors.Length, length);
            var matrixImage = new Matrix<float>(imageDescriptors.Length, length);

            //copy model descriptors into matrixModel
            int row = 0;
            foreach (var modelDescriptor in modelDescriptors)
            {
                for (int i = 0; i < modelDescriptor.Descriptor.Length; i++)
                {
                    matrixModel[row, i] = modelDescriptor.Descriptor[i];
                }

                row++;
            }

            //copy image descriptors into matrixImage
            row = 0;
            foreach (var imageDescriptor in imageDescriptors)
            {
                for (int i = 0; i < imageDescriptor.Descriptor.Length; i++)
                {
                    matrixImage[row, i] = imageDescriptor.Descriptor[i];
                }

                row++;
            }

            //create return matrices for KnnSearch
            var indices = new Matrix<int>(modelDescriptors.Length, 2);
            var dists = new Matrix<float>(modelDescriptors.Length, 2);

            //create our flannIndex
            var flannIndex = new Index(matrixImage);

            //do the search
            flannIndex.KnnSearch(matrixModel, indices, dists, 2, 2);

            //filter out all unnecessary pairs based on distance between pairs
            int pairCount = 0;
            for (int i = 0; i < indices.Rows; i++)
            {
                if (dists.Data[i, 0] < 0.6 * dists.Data[i, 1])
                {
                    ptPairs.Add(new KeyValuePair<int, int>(i, indices.Data[i, 0]));
                    pairCount++;
                }
            }
            //return the pair count
            return pairCount;
        }
        
        /// <summary>
        /// 匹配較快速但精確度較低
        /// </summary>
        /// <param name="template">樣板的特徵點類別</param>
        /// <param name="observedScene">被觀察的場景匹配的特徵點</param>
        /// <returns>回傳匹配的資料類別</returns>
        public static SURFMatchedData MatchSURFFeatureByFLANN(SURFFeatureData template, SURFFeatureData observedScene)
        {
            Matrix<byte> mask;
            int k = 2;
            double uniquenessThreshold = 0.3;
            Matrix<int> indices;
            HomographyMatrix homography = null;
            Stopwatch watch;
            Matrix<float> dists;

            try
            {
                watch = Stopwatch.StartNew();
                #region FLANN Match CPU
                //match 
                Index flann = new Index(template.GetDescriptors(), 4);

                indices = new Matrix<int>(observedScene.GetDescriptors().Rows, k);
                using (dists = new Matrix<float>(observedScene.GetDescriptors().Rows, k))
                {
                    flann.KnnSearch(observedScene.GetDescriptors(), indices, dists, k, 2);
                    mask = new Matrix<byte>(dists.Rows, 1);
                    mask.SetValue(255);
                    Features2DToolbox.VoteForUniqueness(dists, uniquenessThreshold, mask);
                }
                int nonZeroCount = CvInvoke.cvCountNonZero(mask);
                Console.WriteLine("-----------------\nVoteForUniqueness pairCount => " + nonZeroCount.ToString() + "\n-----------------");
                if (nonZeroCount >=4) //原先是4
                {
                    nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(template.GetKeyPoints(), observedScene.GetKeyPoints(), indices, mask, 1.2, 30);
                    Console.WriteLine("VoteForSizeAndOrientation pairCount => " + nonZeroCount.ToString() + "\n-----------------");
                    //filter out all unnecessary pairs based on distance between pairs

                    if (nonZeroCount >= 30) //原先是4
                        homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(template.GetKeyPoints(), observedScene.GetKeyPoints(), indices, mask, 5); //原先是5

                }
                #endregion
                watch.Stop();
                Console.WriteLine("Cal SURF Match time => " + watch.ElapsedMilliseconds.ToString() + "\n-----------------");


                return new SURFMatchedData(indices, homography, mask, nonZeroCount,template) ;
            }
            catch (CvException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ErrorMessage);
                return null;
            }
        }

        /// <summary>
        /// 使用BruteForce匹配(較精確但較慢)
        /// </summary>
        /// <param name="template">樣板的特徵點類別</param>
        /// <param name="observedScene">被觀察的場景匹配的特徵點</param>
        /// <returns>回傳匹配的資料類別</returns>
        public static SURFMatchedData MatchSURFFeatureByBruteForce(SURFFeatureData template, SURFFeatureData observedScene)
        {
            Matrix<byte> mask;
            int k = 2;
            double uniquenessThreshold = 0.5; //default:0.8
            Matrix<int> indices;
            HomographyMatrix homography = null;
            Stopwatch watch;
            try
            {
                watch = Stopwatch.StartNew();
                #region bruteForce match for CPU
                //match 
                BruteForceMatcher<float> matcher = new BruteForceMatcher<float>(DistanceType.L2Sqr); //default:L2
                matcher.Add(template.GetDescriptors());

                indices = new Matrix<int>(observedScene.GetDescriptors().Rows, k);
                using (Matrix<float> dist = new Matrix<float>(observedScene.GetDescriptors().Rows, k))
                {
                    matcher.KnnMatch(observedScene.GetDescriptors(), indices, dist, k, null);
                    mask = new Matrix<byte>(dist.Rows, 1);
                    mask.SetValue(255);
                    Features2DToolbox.VoteForUniqueness(dist, uniquenessThreshold, mask);
                }

                int nonZeroCount = CvInvoke.cvCountNonZero(mask);
                Console.WriteLine("-----------------\nVoteForUniqueness pairCount => " + nonZeroCount.ToString() + "\n-----------------");
                if (nonZeroCount >= 4)
                {
                    nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(template.GetKeyPoints(), observedScene.GetKeyPoints(), indices, mask, 1.5, 30); //default:1.5 , 10, scale increment:1.5 rotatebin:50
                    Console.WriteLine("VoteForSizeAndOrientation pairCount => " + nonZeroCount.ToString() + "\n-----------------");
                    if (nonZeroCount >= 25) //defalut :4 , modify: 15
                        homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(template.GetKeyPoints(), observedScene.GetKeyPoints(), indices, mask, 5);
                }
                #endregion
                watch.Stop();
                Console.WriteLine("Cal SURF Match time => " + watch.ElapsedMilliseconds.ToString() + "\n-----------------");

                return new SURFMatchedData(indices, homography, mask, nonZeroCount,template);
            }
            catch (CvException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ErrorMessage);
                return null;
            }
        }
        /// <summary>
        /// 取得對應出物體的ROI座標點
        /// </summary>
        /// <param name="homography">保存了相關匹配後的資訊(用來投影至商品上的匹配位置)</param>
        /// <param name="template">樣板特徵類別</param>
        /// <returns>回傳座標點</returns>
        public static PointF[] GetMatchBoundingBox(HomographyMatrix homography, SURFFeatureData template)
        {
            if (homography != null) //Get RoI box
            {
                //draw a rectangle along the projected model    
                PointF[] pts = new PointF[] { 
                        new PointF(template.GetImg().ROI.Left, template.GetImg().ROI.Bottom),
                        new PointF(template.GetImg().ROI.Right, template.GetImg().ROI.Bottom),
                        new PointF(template.GetImg().ROI.Right, template.GetImg().ROI.Top),
                        new PointF(template.GetImg().ROI.Left, template.GetImg().ROI.Top)
                };
                homography.ProjectPoints(pts); //project points
                return pts;
            }
            else
                return null;
        }
        /// <summary>
        /// 顯示畫出匹配的視窗
        /// </summary>
        /// <param name="matchData">匹配後回傳的資料類別</param>
        /// <param name="observedScene">觀察景象特徵資料</param>
        public static void ShowSURFMatchForm(SURFMatchedData matchData, SURFFeatureData observedScene) 
        {
            PointF[] matchPts = GetMatchBoundingBox(matchData.GetHomography(), matchData.GetTemplateSURFData());
            //Draw the matched keypoints
            Image<Bgr, Byte> result = Features2DToolbox.DrawMatches(matchData.GetTemplateSURFData().GetImg(), matchData.GetTemplateSURFData().GetKeyPoints(), observedScene.GetImg(), observedScene.GetKeyPoints(),
                matchData.GetIndices(), new Bgr(255, 255, 255), new Bgr(255, 255, 255), matchData.GetMask(), Features2DToolbox.KeypointDrawType.DEFAULT);
            if (matchPts != null)
            {
                result.DrawPolyline(Array.ConvertAll<PointF, Point>(matchPts, Point.Round), true, new Bgr(Color.Red), 2);
            }
            new ImageViewer(result, "顯示匹配圖像").Show();
        }
    }
}
