using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.UI;
using Emgu.CV.Util;
using LedChipPassFail_first.Data;
using LedChipPassFail_first.Func;
using System.Windows.Media;
using System.Windows.Controls;
using System.IO;
using System.Runtime.Serialization.Json;
using static Emgu_processingTool.Vision_Tool;
using static Emgu_processingTool.Preprocessing;
using static Util_Tool.UI.Corrdinate;

namespace LedChipPassFail_first
{
    public partial class MainCore
    {
        #region Init
        public MainCore()
        {
            PData = new ImgPData();
            PResult = new ImgPResult();
        }

        public void InitClass()
        {
            PData = new ImgPData();
            PResult = new ImgPResult();
        }


        #endregion


        #region Save & Load

        //public void SaveImg ( Image<Bgr,byte> img , string path )
        //{
        //    img.Save( path );
        //}

        public void SaveImg( dynamic img , string path )
        {
            img.Save( path );
        }

        public void SaveData( ImgPResult result , string path) {
            string delimiter = ",";
            StringBuilder csvExport = new StringBuilder(); //
            csvExport.Append( "Pass Number" );
            csvExport.Append( delimiter );
            csvExport.Append( PResult.ChipPassCount.ToString() );
            csvExport.Append( delimiter );
            csvExport.Append( "Fail Number" );
            csvExport.Append( delimiter );
            csvExport.Append( PResult.ChipFailCount.ToString() );
            csvExport.Append( Environment.NewLine );
            csvExport.Append( "Y (Row)" );
            csvExport.Append( delimiter );
            csvExport.Append( "X (Column)" );
            csvExport.Append( delimiter );
            csvExport.Append( "OK/NG)" );
            csvExport.Append( delimiter );
            csvExport.Append( "Size" );
            csvExport.Append( delimiter );
            csvExport.Append( "Intensity" );
            csvExport.Append( delimiter );
            csvExport.Append( Environment.NewLine );

            for ( int i = 0 ; i < result.OutData.Count ; i++ )
            {
                csvExport.Append( result.OutData[i].Hindex+1);
                csvExport.Append( delimiter );
                csvExport.Append( result.OutData[i].Windex+1 );
                csvExport.Append( delimiter );
                csvExport.Append( result.OutData[i].PassFail? "OK":"NG" );
                csvExport.Append( delimiter );
                csvExport.Append( result.OutData[i].ContourSize );
                csvExport.Append( delimiter );
                csvExport.Append( result.OutData[i].Intensity);
                csvExport.Append( Environment.NewLine );
            }
            System.IO.File.WriteAllText( path , csvExport.ToString() );
        }
        #endregion

        //public void SaveConfig(ConfigData data)
        //{
        //    MemoryStream strm = new MemoryStream();
        //    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ConfigData));
        //    ser.WriteObject( strm , data );
        //
        //
        //
        //}




    }
}
