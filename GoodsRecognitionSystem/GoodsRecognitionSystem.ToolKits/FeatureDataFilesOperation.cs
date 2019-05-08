using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
//file IO
using System.IO;
//xml reader
using System.Xml;
//xml XmlSerializer
using System.Xml.Serialization;
//EmguCV
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using GoodsRecognitionSystem.ToolKits.SURFMethod;

namespace GoodsRecognitionSystem.ToolKits
{
    /// <summary>
    /// 特徵與值方圖的檔案讀寫工具類別
    /// </summary>
    public static  class FeatureDataFilesOperation
    {
        /// <summary>
        /// 寫成二位元檔案
        /// </summary>
        /// <param name="surf">SURF特徵類別</param>
        /// <param name="TextFileName">檔案的路徑名稱</param>
        public static void WriteSURFFeatureDataToBinaryXml(SURFFeatureData surf, string TextFileName)
        {
            Stream stream;
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter;
            //寫檔
            try
            {
                // serialize histogram
                stream = File.Open(TextFileName, FileMode.Create);
                bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bformatter.Serialize(stream, surf);
                stream.Close();
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
        /// <summary>
        /// 寫成二位元檔案
        /// </summary>
        /// <param name="histDense">值方圖類別</param>
        /// <param name="TextFileName">檔案的路徑名稱</param>
        public static void WriteHistogramDataToBinaryXml(DenseHistogram histDense, string TextFileName)
        {

            Stream stream;
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter;
            //寫檔
            try
            {
                // serialize histogram
                stream = File.Open(TextFileName, FileMode.Create);
                bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bformatter.Serialize(stream, histDense);
                stream.Close();
            }

            catch (IOException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        /// <summary>
        /// 讀取SURFFeature類別,寫入的資料是序列Byte
        /// </summary>
        /// <param name="TextFileName"></param>
        /// <returns></returns>
        public static SURFFeatureData ReadSURFFeatureDataFromBinaryXml(string TextFileName)
        {
            Stream stream;
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter;
            SURFFeatureData templateSurf;
            try
            {
                if (File.Exists(TextFileName))
                {
                    stream = File.Open(TextFileName, FileMode.Open);
                    bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    templateSurf = (SURFFeatureData)bformatter.Deserialize(stream);
                    stream.Close();
                    return templateSurf;
                }
                return null;

            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        /// <summary>
        /// 讀取XML值方圖,寫入的資料是序列Byte
        /// </summary>
        /// <param name="TextFileName">檔案路徑名稱</param>
        /// <returns></returns>
        public static DenseHistogram ReadHistogramDataFromBinaryXml(string TextFileName)
        {

            Stream stream;
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bformatter;
            DenseHistogram histLoaded;
            try
            {
                if (File.Exists(TextFileName))
                {
                    stream = File.Open(TextFileName, FileMode.Open);
                    bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    histLoaded = (DenseHistogram)bformatter.Deserialize(stream);
                    stream.Close();
                    return histLoaded;
                }
                return null;

            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
    }
}
