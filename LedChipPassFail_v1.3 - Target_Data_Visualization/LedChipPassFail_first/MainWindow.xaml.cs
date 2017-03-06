using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.UI;
using Emgu.CV.Util;
using LedChipPassFail_first.Data;
using LedChipPassFail_first.Func;
using System.Diagnostics;
using Accord.Math.Metrics;
using static Emgu_processingTool.ThresholdMode;
using Microsoft.VisualStudio.DebuggerVisualizers;
using Emgu_processingTool;
using Util_Tool.FileIO.Csv;

namespace LedChipPassFail_first
{
    public enum TargetDraw { On, Off };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        MainCore Core = new MainCore();
        HistogramBox HistoBox;
        DenseHistogram[] HistogramList;
        WindowsFormsHost WinHost;

        ImageBrush[] CornerImgs { get {return new ImageBrush[4] { imgLT , imgLB , imgRT , imgRB }; } }
        Canvas[] CornerCanvs { get { return new Canvas[4] { canvasLT , canvasLB , canvasRT , canvasRB }; } }
        Border[] CornerBorder { get { return new Border[4] { borderLT , borderLB , borderRT , borderRB }; } }

        Func<Image<Gray , byte> , Image<Gray , byte>>[] CornerImgCrops;
        Action<object, MouseButtonEventArgs>[] CornerClickEvt;
        bool ProcessDone = false;

        #region Init
        public MainWindow()
        {
            InitializeComponent();
            InitDisplay();
        }


        // init //
        void InitCornerFunc() {
            CornerImgCrops = new Func<Image<Gray , byte> , Image<Gray , byte>>[4] {
                Core.CropImgLT, Core.CropImgLB , Core.CropImgRT , Core.CropImgRB };
            CornerClickEvt = new Action<object , MouseButtonEventArgs>[4] { LTClickEvt , LBClickEvt , RTClickEvt , RBClickEvt };
        }

        void InitDisplay()
        {
            nudCWNum.Value = 120;
            nudCHNum.Value = 180;
            nudThresh.Value = 40;
            nudAreaUpLimit.Value = 109;
            nudAreaDWLimit.Value = 5;

            stpFull.Background = Core.FailBrush[Core.LGFactorName[0]];
            stpIR5V.Background = Core.FailBrush[Core.LGFactorName[1]];
            stpVF1.Background  = Core.FailBrush[Core.LGFactorName[2]];
            stpVF2.Background  = Core.FailBrush[Core.LGFactorName[3]];
            stpVF3.Background  = Core.FailBrush[Core.LGFactorName[4]];
            stpVF1IR.Background  = Core.FailBrush[Core.LGFactorName[5]];
        }
        void ClearLRFrame()
        {
            canvasLT.Children.Clear();
            canvasLB.Children.Clear();
            canvasRT.Children.Clear();
            canvasRB.Children.Clear();
            canvasProced.Children.Clear();
            canvasIndex.Visibility = Visibility.Hidden;
            borderIndex.Visibility = Visibility.Hidden;
            OpenCornerImg( CornerCanvs );
        }
       
        void SetInitImg(ImageBrush[] cornerImg ,Canvas oriCanvas , Canvas Pro , Canvas[] cornerCanv)
        {
            List<Rectangle> rectList = new List<Rectangle>();

            /*Canvas Setting*/
            double[] canvXYLen = Core.MapImg2Canv( new double[2] { Core.LTRBPixelNumberH , Core.LTRBPixelNumberW} );
            Core.SetCornerRect( oriCanvas , canvXYLen[0] , canvXYLen[1] );
            RenderOptions.SetBitmapScalingMode( Pro , BitmapScalingMode.NearestNeighbor );

            for ( int i = 0 ; i < 4 ; i++ )
            {
                RenderOptions.SetBitmapScalingMode( cornerCanv[i] , BitmapScalingMode.NearestNeighbor );
                var temp =  CornerImgCrops[i]( Core.OriginImg );
                //ImageViewer.Show( temp , "Test Window" );
                cornerImg[i].ImageSource = BitmapSrcConvert.ToBitmapSource( CornerImgCrops[i]( Core.OriginImg ) );
                cornerCanv[i].MouseLeftButtonUp += new MouseButtonEventHandler( CornerClickEvt[i] );
            }
            imgOri.ImageSource = BitmapSrcConvert.ToBitmapSource( Core.OriginImg );
        }

        #region corner click evt
        void LTClickEvt( object ob , MouseButtonEventArgs ev )
        {
            while ( canvasLT.Children.Count > 0 ) { canvasLT.Children.RemoveAt( canvasLT.Children.Count - 1 ); }

            double py = ev.GetPosition( this.canvasLT ).Y  ;
            double px = ev.GetPosition( this.canvasLT ).X  ;

            Core.PData.LTPos_Img = Core.MapCanv2ImgLTRB( new double[2] { py,px } );

            Rectangle rect =  StartEndDot(py,px);
            canvasLT.Children.Add( rect );
        }

        void LBClickEvt( object ob , MouseButtonEventArgs ev )
        {
            while ( canvasLB.Children.Count > 0 ) { canvasLB.Children.RemoveAt( canvasLB.Children.Count - 1 ); }

            double py = ev.GetPosition( this.canvasLB ).Y    ;
            double px = ev.GetPosition( this.canvasLB ).X    ;
            double[] onCropImgPos = Core.MapCanv2ImgLTRB( new double[2] { py,px } );

            Core.PData.LBPos_Img = new double[2] { onCropImgPos[0] - Core.LTRBPixelNumberH + Core.OriginImg.Height , onCropImgPos[1] };
            Rectangle rect =  StartEndDot(py, px);
            canvasLB.Children.Add( rect );
        }

        void RTClickEvt( object ob , MouseButtonEventArgs ev )
        {
            while ( canvasRT.Children.Count > 0 ) { canvasRT.Children.RemoveAt( canvasRT.Children.Count - 1 ); }

            double py = ev.GetPosition( this.canvasRT ).Y  ;
            double px = ev.GetPosition( this.canvasRT ).X  ;
            double[] onCropImgPos = Core.MapCanv2ImgLTRB( new double[2] { py, px } );

            Core.PData.RTPos_Img = new double[2] { onCropImgPos[0] , onCropImgPos[1] - Core.LTRBPixelNumberW + Core.OriginImg.Width };
            Rectangle rect =  StartEndDot(py,px);
            canvasRT.Children.Add( rect );
        }

        void RBClickEvt( object ob , MouseButtonEventArgs ev )
        {
            while ( canvasRB.Children.Count > 0 ) { canvasRB.Children.RemoveAt( canvasRB.Children.Count - 1 ); }

            double py = ev.GetPosition( this.canvasRB ).Y  ;
            double px = ev.GetPosition( this.canvasRB ).X  ;
            double[] onCropImgPos = Core.MapCanv2ImgLTRB( new double[2] { py, px } );

            Core.PData.RBPos_Img = new double[2] { onCropImgPos[0] - Core.LTRBPixelNumberH + Core.OriginImg.Height , onCropImgPos[1] - Core.LTRBPixelNumberW + Core.OriginImg.Width };
            Rectangle rect =  StartEndDot(py,px);
            canvasRB.Children.Add( rect );
        }

        Rectangle StartEndDot( double py , double px )
        {
            Rectangle rect = new Rectangle();
            rect.Width = 4;
            rect.Height = 4;
            rect.StrokeThickness = 1;
            rect.Fill = new SolidColorBrush( Colors.OrangeRed );
            rect.Stroke = new SolidColorBrush( Colors.OrangeRed );
            Canvas.SetLeft( rect , px - rect.Width/2 );
            Canvas.SetTop( rect , py - rect.Height/2 );
            return rect;
        }
        #endregion


        #endregion


        #region MainFunction Button Evt

        private async void btnLoad_Click( object sender , RoutedEventArgs e )
        {
           

            OpenFileDialog ofd = new OpenFileDialog();
            if ( ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK )
            {
                ClearLRFrame(); // Canvas Clear and reset
                TestFileSavePath.Setting( ofd.FileName );
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                await Task.Run( () => {
                    //Mat graymat  = new Mat(ofd.FileName,ImreadModes.Grayscale);
                    //Mat colormat = new Mat(ofd.FileName,ImreadModes.Color);
                    //
                    //Core.OriginImg = graymat.ToImage<Gray , byte>( false );
                    //Core.ColorOriImg = colormat.ToImage<Bgr , byte>( false );
                    Core.OriginImg = new Image<Gray , byte>(ofd.FileName);
                    Core.ColorOriImg = new Image<Bgr , byte>(ofd.FileName);
                
                } );
                Core.InitGFunc( canvas , CornerCanvs[0] );
                InitCornerFunc();
                SetInitImg( CornerImgs , canvas , canvasProced , CornerCanvs  );
                Core.PData.SetFrame( canvas.ActualHeight , canvas.ActualWidth , Core.OriginImg.Height , Core.OriginImg.Width );
                Mouse.OverrideCursor = null;
            }
        }
        
        private async void btnStartProcssing_Click( object sender , RoutedEventArgs e )
        {
            this.BeginInvoke( () => Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait );
            var result = CheckNGMode( txbFactorName.Text );
            if(ReadyProc()) await Task.Run(()=> Core.ProcessingStep1( result )() );
            this.BeginInvoke( () => {
                imgPro.ImageSource = BitmapSrcConvert.ToBitmapSource( Core.ProcedImg );
                imgIndex.ImageSource = BitmapSrcConvert.ToBitmapSource( Core.IndexViewImg );
                Mouse.OverrideCursor = null;
            } );

            lblPassChipnum.BeginInvoke( () => lblPassChipnum.Content = Core.PResult.ChipPassCount );
            lblFailChipnum.BeginInvoke( () => lblFailChipnum.Content = Core.PResult.ChipFailCount );
            lblTotalChip.BeginInvoke( ()=> lblTotalChip.Content = Core.PData.ChipHNum * Core.PData.ChipWNum );

            //DisplayResultHisto(Core.PResult);
        }
        private void btnSaveData_Click( object sender , RoutedEventArgs e )
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if ( sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK )
            {
                Core.SaveData( Core.PResult , sfd.FileName + ".csv" );
            }
        }
        private void btnSaveImg_Click( object sender , RoutedEventArgs e )
        {
            try
            {

                SaveFileDialog sfd = new SaveFileDialog();
                if ( sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK )
                {
                    var resizedimg = Core.IndexViewImg.Resize( Core.ProcedImg.Width , Core.ProcedImg.Height , Inter.Nearest );
                    Core.SaveImg( Core.IndexViewImg , sfd.FileName + "_OverView_Point2Chip.png" );
                    Core.SaveImg( resizedimg , sfd.FileName + "_OverView_SameSize.png" );
                    Core.SaveImg( Core.ProcedImg , sfd.FileName + "_Proced.png" );

                    HistogramList[0]?.Save( sfd.FileName + "_Histogram1.png" );
                    HistogramList[1]?.Save( sfd.FileName + "_Histogram2.png" );
                }

            }
            catch ( Exception )
            {
            }
        }

        #endregion



        #region Process Ready
        bool ReadyProc()
        {
            foreach ( var pos in Core.PData.CornerPos_Img )
            {
                if ( pos == null) 
                {
                    System.Windows.Forms.MessageBox.Show( "Set First and Last Chip Position First" );
                    Mouse.OverrideCursor = null;
                    return false;
                }
            }

            SetProcessingData();
            ChangeFront2ImgProcStep();
            CreateFuncofProc();
            HideCornerImg( CornerCanvs );
            canvasIndex.Visibility = Visibility.Visible;
            borderIndex.Visibility = Visibility.Visible;
            
            //-> Check Draw LGsample Factor
            if ( Core.TargetData != null && txbFactorName.Text != "" )
            {
                string[] textTemp = txbFactorName.Text.Split(',').ToArray<string>();
                Core.CurrFactor = textTemp.Where( x => Core.LGFactorName.Contains( x ) ).Select( x => x ).ToArray();
            }
            else Core.CurrFactor = null;

            return true;
        }

        void HideCornerImg(Canvas[] canvases) {
            for ( int i = 0 ; i < 4 ; i++ )
            {
                canvases[i].Visibility = Visibility.Hidden;
                CornerBorder[i].Visibility = Visibility.Hidden;
            }
        }

        void OpenCornerImg( Canvas[] canvases ) {
            for ( int i = 0 ; i < 4 ; i++ )
            {
                canvases[i].Visibility = Visibility.Visible;
                CornerBorder[i].Visibility = Visibility.Visible;
            }
        }
        
        void SetProcessingData()
        {
            Core.PData.ImgRealH = Core.OriginImg.Height;
            Core.PData.ImgRealW = Core.OriginImg.Width;
            Core.PData.CanvasH = ( int ) canvas.ActualHeight;
            Core.PData.CanvasW = ( int ) canvas.ActualWidth;

            Core.PData.ChipWNum = ( int ) nudCWNum.Value;
            Core.PData.ChipHNum = ( int ) nudCHNum.Value;

            Core.PData.ThresholdV = ( int ) nudThresh.Value;
            Core.PData.UPAreaLimit = ( int ) ( nudAreaUpLimit.Value );
            Core.PData.DWAreaLimit = ( int ) ( nudAreaDWLimit.Value );
        }
        void ChangeFront2ImgProcStep()
        {
            btnStartProcssing.IsEnabled = true;
            Removeevent( CornerCanvs );
            ClearLRFrame();
            while ( canvas.Children.Count > 0 ) { canvas.Children.RemoveAt( canvas.Children.Count - 1 ); } // delect rect
            titleRB.Text = "Histogram";
            titleLT.Text = "Indexing View";

            Core.Create_EstedChipPos( Core.PData.CornerPos_Img , ckbEst4Pos.IsChecked.Value ? EstChipPosMode.With4Point : EstChipPosMode.With2Point );

            Core.IndexViewImg = new Image<Bgr , byte>( Core.PData.ChipWNum , Core.PData.ChipHNum );
            Core.IndexViewImg.Data = MatPattern( Core.PData.ChipHNum , Core.PData.ChipWNum , 3 );
            imgIndex.ImageSource = BitmapSrcConvert.ToBitmapSource( Core.IndexViewImg );
            imgRB.ImageSource = null;

            WinHost = CreateWinHost(canvasLT);
            HistoBox = new HistogramBox();

            canvasRB.Children.Clear();
            AddHist2Box( HistoBox , ref HistogramList, HistoFromImage( Core.OriginImg , Core.BinSize ) ,
                             ( bool ) ckbSetHistRange.IsChecked ? float.Parse( nudHistDW.Text ) : 0 ,
                             ( bool ) ckbSetHistRange.IsChecked ? float.Parse( nudHistUP.Text ) : 255 );
            HistoBox.Refresh();
            WinHost.Child = HistoBox;
            canvasRB.Children.Add( WinHost );
        }
        void CreateFuncofProc()
        {
            ThresholdMode mode = ckbThresMode.IsChecked.Value ? ThresholdMode.Auto : ThresholdMode.Manual;
            Core.CreateProcFun( mode );
        }
        
        System.Drawing.Rectangle CenterDotForDrawing( double px , double py )
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle();
            rect.Width = 2;
            rect.Height = 2;
            return rect;
        }
        #endregion



        #region After Setting Function
        List<TargetNG> CheckNGMode(string ngstringInput) {
            string[] selected = ngstringInput.Split(',');
            List<TargetNG> output = new List<TargetNG>();
            foreach( var item in Core.LGFactorName ) {
                if( selected.Contains( item ) ) output.Add( Core.TargetNgBook[item] );
            }
            return output;
        }

        Image<Bgr,byte> DrawContour(Image<Bgr,byte> img,VectorOfVectorOfPoint contr) {
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
                    img.Data[( int ) centrPoint[i , j , 0] , ( int ) centrPoint[i , j , 1] , 0] = 0;
                    img.Data[( int ) centrPoint[i , j , 0] , ( int ) centrPoint[i , j , 1] , 1] = 0;
                    img.Data[( int ) centrPoint[i , j , 0] , ( int ) centrPoint[i , j , 1] , 2] = 255;
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

        void Removeevent(Canvas[] canvases )
        {
            for ( int i = 0 ; i < 4 ; i++ )
            {
                canvases[i].MouseLeftButtonUp -= new MouseButtonEventHandler( CornerClickEvt[i] );
            }
        }

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

                    if ( i % 2 == 0 ) {
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
                    else if ( j%2 == 0) {
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
        
        #endregion



        #region Histogram

        void DisplayResultHisto(ImgPResult data)
        {
            HistoBox.ClearHistogram();
            HistoBox.Name = "AreaSize";
            AddHist2Box( HistoBox , ref HistogramList, HistoFromResult( data ) ,
                             ( bool ) ckbSetHistRange.IsChecked ? float.Parse( nudHistDW.Text ) : 0 ,
                             ( bool ) ckbSetHistRange.IsChecked ? float.Parse( nudHistUP.Text ) : 255 );
            HistoBox.Refresh();
            WinHost.Child = HistoBox;
            //canvasHist.Children.Clear();
            //canvasHist.Children.Add( WinHost );
            
        }

        void AddHist2Box( HistogramBox box , ref DenseHistogram[] histogramArr,dynamic createhist, float dw, float up)
        {
            histogramArr = createhist( dw , up );
            for ( int i = 0 ; i < histogramArr.GetLength(0) ; i++ )
            {
                if ( histogramArr[i] != null )
                {
                    box.AddHistogram( null , System.Drawing.Color.Black , histogramArr[i] , Core.BinSize , new float[] { dw , up } );
                }
            }
        }

        #region Helper
        WindowsFormsHost CreateWinHost( Canvas targcanv )
        {
            WindowsFormsHost wh = new WindowsFormsHost();
            wh.Width = targcanv.Width;
            wh.Height = targcanv.Height;
            wh.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            wh.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            return wh;
        }

        DenseHistogram CreateHisto( ImgPResult result , Func<List<ExResult> , int[]> func )
        {
            List<int> temp = new List<int>();
            var item = func(result.OutData);

            DenseHistogram hist = new DenseHistogram(20,new RangeF((float)item.Min(),(float)item.Max()));
            Matrix<float> farr = new Matrix<float>(1,item.GetLength(0));
            for ( int i = 0 ; i < item.GetLength( 0 ) ; i++ )
            {
                farr.Data[0 , i] = item[i];
            }
            Matrix<float>[] histData = new Matrix<float>[1] { farr }; // Histogram data is Matrix<float>
            hist.Calculate( histData , true , null );
            return hist;
        }

        Func< float , float , DenseHistogram[]> HistoFromImage( Image<Gray , byte> img , int binsize )
        {
            var fromimg = new Func< float , float , DenseHistogram[]> ( ( dw , up ) =>
            {
                DenseHistogram[] hist = new DenseHistogram[] { };
                hist = new DenseHistogram[1];
                hist[0] = new DenseHistogram( binsize , new RangeF( dw , up ) );
                hist[0].Calculate<byte>( new Image<Gray , byte>[] { img } , true , null );
                return hist;
            } );
            return fromimg;
        }

        Func<float , float ,DenseHistogram[]> HistoFromResult( ImgPResult result )
        {
            var fromresult = new Func<float , float ,DenseHistogram[]>((float dw, float up)=>
            {
                var item = result.OutData.Select( i => ( int ) i.Intensity ).ToArray();
                DenseHistogram histIntes = CreateHisto(Core.PResult, new Func<List<ExResult>,int[]>( j => j.Select(i => ( int ) i.Intensity).ToArray() ));
                DenseHistogram histSize  = CreateHisto(Core.PResult, new Func<List<ExResult>,int[]>( j => j.Select(i => ( int ) i.ContourSize).ToArray() ));
                return new DenseHistogram[2] { histIntes , histSize };
            } );
            return fromresult;
        }

        
        #endregion
        private void ckbSetHistRange_Checked( object sender , RoutedEventArgs e )
        {
            RefreshHistogram();
        }

        private void ckbSetHistRange_Unchecked( object sender , RoutedEventArgs e )
        {
            
            RefreshHistogram();
        }

        void RefreshHistogram()
        {
            try
            {
                if ( !ProcessDone && HistogramList != null )
                {
                    HistoBox.ClearHistogram();
                    AddHist2Box( HistoBox , ref HistogramList, HistoFromImage(Core.OriginImg, Core.BinSize ) ,
                            ( bool ) ckbSetHistRange.IsChecked ? float.Parse( nudHistDW.Text ) : 0 ,
                            ( bool ) ckbSetHistRange.IsChecked ? float.Parse( nudHistUP.Text ) : 255 );
                }
            }
            catch ( Exception )
            {
                System.Windows.Forms.MessageBox.Show( "Please Input only Number on Histogram Range" );
            }
        }
        #endregion

        private void btnLoadTarget_Click( object sender , RoutedEventArgs e )
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if ( ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK )
            {
                Core.CreateTargetData = Core.Create_CreateTargetData( 0 , 1 , 6 );
                Core.TargetData = new Dictionary<int[] , Dictionary<string , dynamic>>( new MyEqualityComparer() );
                Core.TargetData = Core.CreateTargetData( Core.LoadCsv( ofd.FileName ) );
            }
        }
    }
}
