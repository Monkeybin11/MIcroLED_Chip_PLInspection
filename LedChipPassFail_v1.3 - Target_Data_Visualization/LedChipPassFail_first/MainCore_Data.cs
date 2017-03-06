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
using System.Windows.Media;
using Math_Tool.Statistic;

namespace LedChipPassFail_first
{
    public partial class MainCore
    {
        public ImgPData PData;
        public ImgPResult PResult;
        public Statistic_Tool<string,bool> Analysis = new Statistic_Tool<string,bool>();

        public Image<Gray,byte> OriginImg;
        public Image<Bgr,byte> ColorOriImg;
        public Image<Bgr,byte> ProcedImg;
        public Image<Bgr,byte> IndexViewImg;
        public double zoomMax = 20;
        public double zoomMin = 0.2;
        public double zoomSpeed = 0.001;
        public double zoom = 1;
        public readonly double LTRBPixelNumberW = 35;
        public readonly double LTRBPixelNumberH = 35;

        public readonly float HistogramDwRange = 46;
        public readonly int BinSize = 100;
        public List<System.Drawing.PointF> PassChipList;
        public List<System.Drawing.PointF> FailChipList;
        public Dictionary<string,dynamic> Confusion_Matrix;

        public Dictionary<string,TargetNG> TargetNgBook = new Dictionary<string, TargetNG>();
        public Dictionary<int[] , Dictionary<string,dynamic>> TargetData;
        public Dictionary<string,Brush> FailBrush = new Dictionary<string, Brush>();
        public Dictionary<string,Bgr> FailColor = new Dictionary<string, Bgr>();
        public string[] CurrFactor;

        public string[] LGFactorName = new string[6] {
            "Full",
            "IR5V" ,
            "VF1",
            "VF2",
            "VF3",
            "VF1IR"
        };

        void SetTargetNgBook() {
            TargetNgBook.Add("Full"  , TargetNG.Full);
            TargetNgBook.Add("IR5V"  , TargetNG.IR5V);
            TargetNgBook.Add("VF1"   , TargetNG.VF1);
            TargetNgBook.Add("VF2"   , TargetNG.VF2);
            TargetNgBook.Add("VF3"   , TargetNG.VF3);
            TargetNgBook.Add("VF1IR" , TargetNG.VF1IR);
        }
    }
}
