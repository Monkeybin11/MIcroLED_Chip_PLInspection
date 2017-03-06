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
using System.Windows.Media;
using System.Windows.Controls;
using static Emgu_processingTool.Vision_Tool;
using static Emgu_processingTool.Preprocessing;
using static Util_Tool.UI.Corrdinate;
using Emgu_processingTool;
using System.Drawing;
using Util_Tool.FileIO.Csv;
using LedChipPassFail_first.Data;

namespace LedChipPassFail_first
{
    public enum EstChipPosMode { With2Point, With4Point }
    public enum TargetNG { None, Full, IR5V, VF1, VF2, VF3, VF1IR }

    public partial class MainCore
    {
        /*Global Function*/
        public Func<double[],double[]> MapCanv2Img;
        public Func<double[],double[]> MapCanv2ImgLTRB;
        public Func<double[],double[]> MapImg2Canv;

        public Func<Image<Gray,byte>, Image<Gray , byte>> CropImgLT;
        public Func<Image<Gray,byte>, Image<Gray , byte>> CropImgLB;
        public Func<Image<Gray,byte>, Image<Gray , byte>> CropImgRT;
        public Func<Image<Gray,byte>, Image<Gray , byte>> CropImgRB;

        public Func<System.Drawing.Rectangle,double> SumBox;
        public Action<Canvas,double,double> SetCornerRect;
        public Func<double[][],double[][]> SelectCol;

        public Action<double,double,dynamic> DrawNG;

        
        /*Conditional Global Function*/
        public Func<int,int,double> SumAreaPoint;
        public Func<int,int,bool>   CheckTargetNg;

        /*Local Function*/
        public Func<double, double, double[,,]> EstedChipPos;
        public Func<Image<Gray,byte>,VectorOfVectorOfPoint> FindPassContour;
        public Func<System.Drawing.PointF , double> InContour;
        public Func<double , double , bool> InBox;
        public Func<VectorOfVectorOfPoint> Sortcontours;
        public Func<VectorOfVectorOfPoint , List<System.Drawing.Rectangle>> ApplyBox;
        public Func<string[][],Dictionary<int[],Dictionary<string,dynamic>>> CreateTargetData;
        

       

        #region Helper
        byte[,,] MatZeros( int channal1 , int channal2 , int channal3 )
        {
            byte[,,] output = new byte[channal1,channal2,channal3];
            for ( int i = 0 ; i < channal1 ; i++ )
            {
                for ( int j = 0 ; j < channal2 ; j++ )
                {
                    for ( int k = 0 ; k < channal3 ; k++ )
                    {
                        output[i , j , k] = 150;
                    }
                }
            }
            return output;
        }

        byte[,,] MatPattern( int channal1 , int channal2 , int channal3 )
        {
            byte[,,] output = new byte[channal1,channal2,channal3];

            Parallel.For( 0 , channal1 , i => {
                Parallel.For( 0 , channal2 , j => {

                    if ( i % 2 == 0 )
                    {
                        if ( j % 2 == 0 )
                        {
                            output[i , j , 0] = 250;
                            output[i , j , 1] = 250;
                            output[i , j , 2] = 250;
                        }
                        else
                        {
                            output[i , j , 0] = 150;
                            output[i , j , 1] = 150;
                            output[i , j , 2] = 150;
                        }
                    }
                    else if ( j % 2 == 0 )
                    {
                        output[i , j , 0] = 200;
                        output[i , j , 1] = 200;
                        output[i , j , 2] = 200;
                    }
                    else
                    {
                        output[i , j , 0] = 100;
                        output[i , j , 1] = 100;
                        output[i , j , 2] = 100;
                    }

                } );
            } );


            return output;
        }

        Image<Bgr , byte> DrawContour( Image<Bgr , byte> img , VectorOfVectorOfPoint contr )
        {
            for ( int i = 0 ; i < contr.Size ; i++ )
            {
                CvInvoke.DrawContours( img , contr , i , new MCvScalar( 0 , 255 , 0 ) );
            }
            return img;
        }

        Image<Bgr , byte> DrawCenterPoint( Image<Bgr , byte> img , double[,,] centrPoint )
        {
            Parallel.For( 0 , centrPoint.GetLength( 0 ) , i =>
            {
                Parallel.For( 0 , centrPoint.GetLength( 1 ) , j =>
                {
                    CircleF cirp = new CircleF();
                    cirp.Center = new PointF( (float)centrPoint[i , j , 1] , (float)centrPoint[i , j , 0] );
                    img.Draw( cirp , new Bgr( 0 , 0 , 250 ) , 1 );
                    //img.Data[( int ) centrPoint[i , j , 0] , ( int ) centrPoint[i , j , 1] , 0] = 0;
                    //img.Data[( int ) centrPoint[i , j , 0] , ( int ) centrPoint[i , j , 1] , 1] = 0;
                    //img.Data[( int ) centrPoint[i , j , 0] , ( int ) centrPoint[i , j , 1] , 2] = 255;
                } );
            } );
            return img;
        }

        Image<Bgr , byte> DrawBox( Image<Bgr , byte> img , List<System.Drawing.Rectangle> rclist )
        {
            Parallel.For( 0 , rclist.Count , i =>
            {
                img.Draw( rclist[i] , new Bgr( 40 , 165 , 5 ) , 1 );
            } );
            return img;
        }
        #endregion

        #region Function Create
        public void InitGFunc( Canvas canvas , Canvas corner)
        {
            MapCanv2Img = Convt_Window2Real( canvas.Width , canvas.Height , OriginImg.Width , OriginImg.Height );
            MapImg2Canv = Convt_Real2window( canvas.Width , canvas.Height , OriginImg.Width , OriginImg.Height );
            MapCanv2ImgLTRB = Convt_Window2Real( corner.Width , corner.Height , LTRBPixelNumberW , LTRBPixelNumberH );

            CropImgLT = FnCropImg( 0 , 0 , 35 , 35 );
            CropImgLB = FnCropImg( 0 , OriginImg.Height - 35 , 35 , OriginImg.Height );
            CropImgRT = FnCropImg( OriginImg.Width - 35 , 0 , OriginImg.Width , 35 );
            CropImgRB = FnCropImg( OriginImg.Width - 35 , OriginImg.Height - 35 , OriginImg.Width , OriginImg.Height );

            SumBox = FnSumBox( OriginImg );
            SetCornerRect = FnSetCornerRect( new Emgu_processingTool.CornerMode[] {
                Emgu_processingTool.CornerMode.LeftTop,
                Emgu_processingTool.CornerMode.LeftBot,
                Emgu_processingTool.CornerMode.RightTop,
                Emgu_processingTool.CornerMode.RightBot,
            } );
        }

        public void Create_EstedChipPos(double[][] cornerPos, EstChipPosMode estmode ) {
            switch ( estmode ) {
                case EstChipPosMode.With2Point:
                    EstedChipPos = FnEstChipPos_2Point( cornerPos[0] , cornerPos[3] );
                    break;

                case EstChipPosMode.With4Point:
                    EstedChipPos = FnEstChipPos_4Point( cornerPos[0] , cornerPos[1] , cornerPos[2] , cornerPos[3] );
                    break;
            }
        }

        public void CreateProcFun(ThresholdMode mode) {
            double thres =  PData.ThresholdV ;
            double areaup = PData.UPAreaLimit;
            double areadw = PData.DWAreaLimit;
            double cHnum =  PData.ChipHNum   ;
            double cWnum =  PData.ChipWNum   ;
            SumAreaPoint = FnSumAreaPoint( ( int ) PData.ChipHSize , ( int ) PData.ChipWSize , OriginImg );
            FindPassContour = FnFindPassContour( thres , areaup , areadw , mode );
            ApplyBox = FnApplyBox( PData.UPBoxLimit , PData.DWBoxLimit );
        }

        public void Create_Sortcontours(VectorOfVectorOfPoint contours) {
            Sortcontours = FnSortcontours( contours );
        }

        public void Create_Inbox( Rectangle box , int margin )
        {
            InBox = FnInBox(box,margin);
        }

        public void Create_SelectCol(int[] idx) {
            Csv<double> csv = new Csv<double>();
            SelectCol = csv.SelectColData( idx );
        }

        public string[][] LoadCsv(string path) {
            Csv<string> csv = new Csv<string>();
            return csv.LoadCsv( ',' )( path );
        }

        /*This function is special function for LG Innotek Data sample*/
        public Func<string[][] , Dictionary<int[] , Dictionary<string , dynamic>>> Create_CreateTargetData( int rowIdx, int colIdx, int labelIdx) {
            // Original Data => Dictionary < Corrdinate , Dictionary < FactorName , Value> > >
            // Call output[Corrdinate]["VF1"] => Value
            return new Func<string[][] , Dictionary<int[] , Dictionary<string , dynamic>>>((data)=> {
                var output = new Dictionary<int[] , Dictionary<string , dynamic>>(new MyEqualityComparer());
                for ( int i = 0 ; i < data.GetLength(0) ; i++ )
                {
                    var content = new Dictionary<string,dynamic>();
                    content.Add( "Label" , data[i][labelIdx]);
                    content.Add( LGFactorName[1] , Convert.ToDouble( data[i][2] ));
                    content.Add( LGFactorName[2] , Convert.ToDouble( data[i][3] ));
                    content.Add( LGFactorName[3] , Convert.ToDouble( data[i][4] ));
                    content.Add( LGFactorName[4] , Convert.ToDouble( data[i][5] ));

                    output.Add( new int[2] { Convert.ToInt32(data[i][rowIdx]) - 1  , Convert.ToInt32( data[i][colIdx] ) - 1} , content );
                }
                return output;
            } );
        }

        public Func<int , int , bool> Create_CheckTargetNg( TargetNG NGType) {
            switch ( NGType ) {
                case TargetNG.None:
                    return new Func<int, int, bool>((row, col)=> { return false; } );

                case TargetNG.Full:
                    return new Func<int , int , bool>((row,col) => {
                        if( TargetData[new int[] { row , col }]["Label"] == "NG" ) return true;
                        else return false;
                    } );

                case TargetNG.IR5V:
                    return new Func<int , int , bool>( ( row , col ) => {
                        if( TargetData[new int[] { row , col }]["IR5V"] < 1.0 ) return true;
                        else return false;
                    } );

                case TargetNG.VF1:
                    return new Func<int , int , bool>( ( row , col ) => {
                        if( TargetData[new int[] { row , col }]["VF1"] < 1.8 ) return true;
                        else return false;
                    } );

                case TargetNG.VF2:
                    return new Func<int , int , bool>( ( row , col ) => {
                        if( TargetData[new int[] { row , col }]["VF2"] < 1.8 ) return true;
                        else return false;
                    } );

                case TargetNG.VF3:
                    return new Func<int , int , bool>( ( row , col ) => {
                        if( TargetData[new int[] { row , col }]["VF3"] < 1.8 ) return true;
                        else return false;
                    } );

                case TargetNG.VF1IR:
                    return new Func<int , int , bool>( ( row , col ) => {
                        var target = TargetData[new int[] { row , col }];
                        if( target["VF1"] < 1.8 || target["IR"] < 1 ) return true;
                        else return false;
                    } );
            }
            return null;
        }

        public Action<double , double , dynamic> Create_DrawNG( TargetNG NGType )
        {
            switch( NGType )
            {
                case TargetNG.None:
                    return new Action<double , double , dynamic>( ( row , col , img) => {} );

                case TargetNG.IR5V:
                    return FnDrawCircle( 1 , FailColor["IR5V"] );

                case TargetNG.Full:
                    return FnDrawCircle( 2 , FailColor["Full"] );

                case TargetNG.VF1:
                    return FnDrawCircle( 3 , FailColor["VF1"] );

                case TargetNG.VF2:
                    return FnDrawCircle( 5 , FailColor["VF2"] );

                case TargetNG.VF3:
                    return FnDrawCircle( 6 , FailColor["VF3"] );

                case TargetNG.VF1IR:
                    return FnDrawCircle( 4 , FailColor["VF1IR"] );
            }
            return null;
        }
        #endregion  
    }

    public class MyEqualityComparer : IEqualityComparer<int[]>
    {
        public bool Equals( int[] x , int[] y )
        {
            if( x.Length != y.Length )
            {
                return false;
            }
            for( int i = 0 ; i < x.Length ; i++ )
            {
                if( x[i] != y[i] )
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode( int[] obj )
        {
            int result = 17;
            for( int i = 0 ; i < obj.Length ; i++ )
            {
                unchecked
                {
                    result = result * 23 + obj[i];
                }
            }
            return result;
        }
    }
}
