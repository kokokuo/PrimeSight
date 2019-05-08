using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//寫檔讀檔
using System.IO;
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
using GoodsRecognitionSystem.ToolKits.SURFMethod;
namespace GoodsRecognitionSystem
{
    /// <summary>
    /// 匹配辨識
    /// </summary>
    public static class MatchRecognition
    {
        #region 讀SURF特徵檔
        //////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 讀取特徵類別檔案
        /// </summary>
        /// <param name="fileName">'檔案路徑'名稱</param>
        /// <returns></returns>
        public static SURFFeatureData ReadSURFFeature(string fileName)
        {
            SURFFeatureData templateSURF = FeatureDataFilesOperation.ReadSURFFeatureDataFromBinaryXml(fileName);
            Console.WriteLine("\n@@@ Read SURF Data........");
            return templateSURF;
        }
        //////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        #region 匹配特徵點
        //////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 比配特徵,對所有檔案匹配並找出最好的匹配檔
        /// </summary>
        /// <param name="surfFiles">載入所有可能作為匹配的特徵資料</param>
        /// <param name="observedImg">要比對觀察的影像</param>
        /// <param name="isDrawMatchForm">是否要顯示出匹配結果</param>
        /// <returns>回傳匹配到的相關資訊類別,String是檔案名稱,如果未匹配到,則Key與Values皆會回傳null,因此要先做檢查</returns>
        public static KeyValuePair<string, SURFMatchedData> MatchSURFFeature(List<string> surfFiles, Image<Bgr, Byte> observedImg, bool isDrawMatchForm)
        {
            Dictionary<string,SURFMatchedData> matchList = new Dictionary<string,SURFMatchedData>();
            SURFFeatureData templateSURFData;
            SURFMatchedData matchedData;
            
            //縮放到一樣大小 (系統修改成可讀圖片時才能加入)
            //observedImg = observedImg.Resize(3, INTER.CV_INTER_LINEAR);

            //1.依序比對
            SURFFeatureData observed = SURFMatch.CalSURFFeature(observedImg);
            Console.WriteLine("### One-by-One Mathed Start.....\n============================");
            foreach (string fileName in surfFiles)
            {
                templateSURFData = MatchRecognition.ReadSURFFeature(fileName);
                Console.WriteLine("SurfData: fileName =>" + Path.GetFileName(fileName));
                matchedData = SURFMatch.MatchSURFFeatureByBruteForce(templateSURFData, observed);
                //如果Homography !=null 表示有匹配到(條件容忍與允許)
                if(matchedData.GetHomography()!=null)
                {
                    matchList.Add(Path.GetFileName(fileName),matchedData);
                }
                Console.WriteLine("match num:" + matchedData.GetMatchedCount().ToString() + "\n-----------------");
            }
            //2.再找出count最大的
            int bestMatched = -1;
            string bestTemplateId = null; //樣板檔案名稱(Id)
            if (matchList.Count != 0)
            {
                foreach (KeyValuePair<string, SURFMatchedData> matchedSURFData in matchList)
                {
                    if (bestMatched == -1 && bestTemplateId == null)
                    {
                        bestMatched = matchedSURFData.Value.GetMatchedCount();
                        bestTemplateId = matchedSURFData.Key;
                    }
                    else
                    {
                        //開始找出最多匹配點的檔案名稱與匹配資訊
                        if (bestMatched < matchedSURFData.Value.GetMatchedCount())
                        {
                            bestMatched = matchedSURFData.Value.GetMatchedCount();
                            bestTemplateId = matchedSURFData.Key;
                        }
                    }
                }
                Console.WriteLine("\n**** Matched fileName=" + bestTemplateId + ", match num:" + bestMatched.ToString() + "****");
                if (isDrawMatchForm)
                    SURFMatch.ShowSURFMatchForm(matchList[bestTemplateId], observed);
                Console.WriteLine("============================\n### Matched Finish.......\n");
                //回傳匹配到的類別
                return new KeyValuePair<string, SURFMatchedData>(bestTemplateId, matchList[bestTemplateId]);
            }
            else 
            {
                Console.WriteLine("\n**** No Matched fileName !");
                if (isDrawMatchForm) 
                {
                    //System.Windows.Forms.MessageBox.Show("No Mathed Goods...!");
                }
                Console.WriteLine("============================\n### Matched Finish.......\n");
                return new KeyValuePair<string, SURFMatchedData>(null, null);
            }
            
           
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
    }
}
