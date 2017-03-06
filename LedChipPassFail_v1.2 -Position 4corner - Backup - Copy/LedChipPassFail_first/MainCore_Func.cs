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

namespace LedChipPassFail_first
{
    public enum EstChipPosMode { With2Point, With4Point }

    public partial class MainCore
    {
        /*Global Function*/
        public Func<double[],double[]> MapCanv2Img;
        public Func<double[],double[]> MapCanv2ImgLTRB;
        public Func<double[],double[]>    MapImg2Canv;
        public Func<Image<Gray,byte>, Image<Gray , byte>> CropImgLT;
        public Func<Image<Gray,byte>, Image<Gray , byte>> CropImgLB;
        public Func<Image<Gray,byte>, Image<Gray , byte>> CropImgRT;
        public Func<Image<Gray,byte>, Image<Gray , byte>> CropImgRB;
        public Func<System.Drawing.Rectangle,double> SumBox;
        public Func<int,int,double> SumAreaPoint;
        public Action<Canvas,double,double> SetCornerRect;
        
        
        
        /*Local Function*/
        public Func<double, double, double[,,]> EstedChipPos;
        public Func<Image<Gray,byte>,VectorOfVectorOfPoint> FindPassContour;
        public Func<System.Drawing.PointF , double> InContour;
        public Func<double , double , bool> InBox;
        public Func<VectorOfVectorOfPoint> Sortcontours;
        public Func<VectorOfVectorOfPoint , List<System.Drawing.Rectangle>> ApplyBox;

        public void InitFunc( Canvas canvas , Canvas corner)
        {
            CropImgLT = FnCropImg( 0 , 0 , 35 , 35 );
            CropImgLB = FnCropImg( 0 , OriginImg.Height - 35 , 35 , OriginImg.Height );
            CropImgRT = FnCropImg( OriginImg.Width - 35 , 0 , OriginImg.Width , 35 );
            CropImgRB = FnCropImg( OriginImg.Width - 35 , OriginImg.Height - 35 , OriginImg.Width , OriginImg.Height );
            MapCanv2Img = Convt_Window2Real( canvas.Width , canvas.Height , OriginImg.Width , OriginImg.Height );
            MapImg2Canv = Convt_Real2window( canvas.Width , canvas.Height , OriginImg.Width , OriginImg.Height );
            MapCanv2ImgLTRB = Convt_Window2Real( corner.Width , corner.Height , LTRBPixelNumberW , LTRBPixelNumberH );
            SumBox = FnSumBox( OriginImg );
            SetCornerRect = FnSetCornerRect( new Emgu_processingTool.CornerMode[] {
                Emgu_processingTool.CornerMode.LeftTop,
                Emgu_processingTool.CornerMode.LeftBot,
                Emgu_processingTool.CornerMode.RightTop,
                Emgu_processingTool.CornerMode.RightBot,
            } );
        }

        public void CreateEstedChipFunc(double[][] cornerPos, EstChipPosMode estmode ) {
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

        public void CreateSortcontour(VectorOfVectorOfPoint contours) {
            Sortcontours = FnSortcontours( contours );
        }

        public void CreateInbox( Rectangle box , int margin )
        {
            InBox = FnInBox(box,margin);
        }

    }
}
