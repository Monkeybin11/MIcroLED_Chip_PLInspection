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
            FailColorInit();
            SetTargetNgBook();
        }

        public void FailColorInit() {
            FailBrush.Add( "IR5V"  , new SolidColorBrush( Color.FromRgb( 198 , 3 , 0     )));
            FailBrush.Add( "VF1"   , new SolidColorBrush( Color.FromRgb( 249 , 255 , 2   )));
            FailBrush.Add( "VF2"   , new SolidColorBrush( Color.FromRgb( 100 , 255 , 255 )));
            FailBrush.Add( "VF3"   , new SolidColorBrush( Color.FromRgb( 255 , 120 , 0   )));
            FailBrush.Add( "VF1IR" , new SolidColorBrush( Color.FromRgb( 150 , 120 , 0   )));
            FailBrush.Add( "Full"  , new SolidColorBrush( Color.FromRgb( 150 , 200 , 90  )));

            FailColor.Add( "IR5V"  , new Bgr( 0   , 3   , 198 ));
            FailColor.Add( "VF1"   , new Bgr( 2   , 255 , 249 ));
            FailColor.Add( "VF2"   , new Bgr( 255 , 255 , 100 ));
            FailColor.Add( "VF3"   , new Bgr( 0   , 120 , 255 ));
            FailColor.Add( "VF1IR" , new Bgr( 0   , 120 , 150 ));
            FailColor.Add( "Full"  , new Bgr( 90  , 200 , 150 ));
        }


        #endregion

        #region Main
        #region Main Function

        public Action ProcessingStep1(List<TargetNG> ngMode)
        {
            return new Action(()=> {
                try
                {
                    PResult = new ImgPResult();

                    /* For Simple Notation */
                    double cHnum =  PData.ChipHNum   ;
                    double cWnum =  PData.ChipWNum   ;
                    Image<Bgr,byte> targetimg = null;

                        /* Create Data */
                    byte[,,] failchipDisplayData = MatPattern((int)cHnum, (int)cWnum , 3);
                    byte[,,] passfailPosData     = new byte[(int)cHnum, (int)cWnum , 1];
                    double[,,] estedChipP        = EstedChipPos( cHnum, cWnum ); // [row,col,0] = rowY , [row,col,1] = colX
                    var passContours  = FindPassContour(OriginImg);
                    PassChipList = new List<System.Drawing.PointF>();
                    FailChipList = new List<System.Drawing.PointF>();
                    targetimg = ColorOriImg.Clone();

                    /* def Draw Ng and Check Ng */
                    List<Action < double , double , dynamic>> DrawNgList = new List<Action<double, double, dynamic>>();
                    List<Func<int , int , bool>> CheckNgList = new List<Func<int , int , bool>>();
                    foreach( var mode in ngMode )
                    {
                        DrawNgList.Add(Create_DrawNG( mode ));
                        CheckNgList.Add(Create_CheckTargetNg( mode ));
                    }

                    /* Draw Contour and Save */
                    //targetimg = DrawContour( targetimg , passContours );
                    //targetimg.Save( TestFileSavePath.ContourName );

                    Create_Sortcontours( passContours );
                    passContours = Sortcontours();
                    var boxlist  = ApplyBox(passContours);
                    targetimg = DrawBox( targetimg , boxlist );
                    //targetimg.Save( TestFileSavePath.BoxName );

                    // Draw EstedPoint on Image and Cavnas 
                    targetimg = DrawCenterPoint( targetimg , estedChipP );


                    #region check pass fail
                    var boximg = ColorOriImg.Clone();
                    for ( int j = 0 ; j < estedChipP.GetLength( 0 ) ; j++ ) // row
                    {
                        for( int i = 0 ; i < estedChipP.GetLength( 1 ) ; i++ ) // col
                        {
                            
                            /*Draw Target Ng*/
                            for( int q = 0 ; q < DrawNgList.Count ; q++ )
                            {
                                if(CheckNgList[q]( j,i )) DrawNgList[q]( estedChipP[j , i , 0] , estedChipP[j , i , 1] , targetimg );
                            }

                            bool isFail = true;
                            for ( int k = 0 ; k < boxlist.Count ; k++ )
                            {
                                /* Check Ested Chip Pos in Contour*/
                                Create_Inbox( boxlist[k] , 1 );
                                if ( InBox( estedChipP[j , i, 0] , estedChipP[j , i , 1] ) )
                                {
                                    PResult.OutData.Add( new ExResult( j , i , true , SumBox( boxlist[k] ) , CvInvoke.ContourArea( passContours[k] ) ) );
                                    PassChipList.Add( new System.Drawing.PointF( ( float ) estedChipP[j , i , 0] , ( float ) estedChipP[j , i , 1] ) );
                                    isFail = false;
                                    break;
                                }
                            }
                            if ( isFail )
                            {
                                double failboxInten = SumAreaPoint( (int)estedChipP[j , i , 0] ,  (int)estedChipP[j , i , 1]);
                                PResult.OutData.Add( new ExResult( j , i , false , failboxInten , 0 ) );
                                FailChipList.Add( new System.Drawing.PointF( ( float ) estedChipP[j , i , 0] , ( float ) estedChipP[j , i , 1] ) );
                                SetFailColor( failchipDisplayData , j , i );
                            }
                        }
                    }

                    IndexViewImg.Data = failchipDisplayData;
                    ProcedImg = targetimg;
                    PResult.ChipPassCount = PassChipList.Count();
                    PResult.ChipFailCount = FailChipList.Count();
                    #endregion
                }
                catch ( Exception er )
                {
                    System.Windows.Forms.MessageBox.Show( er.ToString() );
                }
            } );
        }
        void SetFailColor( byte[,,] failchipDisplayData , int j , int i )
        {
            failchipDisplayData[j , i , 0] = ( byte ) ( failchipDisplayData[j , i , 0] * 0.3 );
            failchipDisplayData[j , i , 1] = ( byte ) ( failchipDisplayData[j , i , 1] * 0.5 );
            failchipDisplayData[j , i , 2] = 200;
        }
        #endregion

        public void Analysis_Processing()
        {
            string[] target;
            bool[] predict;

            Load_Target_Predict( out target , out predict );
            if( target != null )
            {
                var compare = Analysis.Convert2intLabel( target , predict , "OK" , true );
                Confusion_Matrix = Analysis.ConfusionMatrix( compare["Target"] , compare["Predict"] );
            }
        }

        void Load_Target_Predict(out string[] target ,out bool[] predict ) {
            if( TargetData.Count > 0 )
            {
                target = new string[PResult.OutData.Count];
                predict = new bool[PResult.OutData.Count];

                for( int i = 0 ; i < PResult.OutData.Count ; i++ )
                {
                    var y = PResult.OutData[i].Hindex;
                    var x = PResult.OutData[i].Windex;
                    predict[i] = PResult.OutData[i].PassFail;
                    target[i] = TargetData[new int[2] { y , x }]["Label"];
                }
            }
            else
            {
                target = null;
                predict = null;
            }
        }
        #endregion

        #region Save & Load

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
            csvExport.Append( "Pass/Fail)" );
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
                csvExport.Append( result.OutData[i].Windex +1);
                csvExport.Append( delimiter );
                csvExport.Append( result.OutData[i].PassFail? "Pass":"Fail" );
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
