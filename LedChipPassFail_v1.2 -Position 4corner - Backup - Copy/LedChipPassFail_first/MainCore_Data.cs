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

namespace LedChipPassFail_first
{
    public partial class MainCore
    {
        public ImgPData PData;
        public ImgPResult PResult;
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
        public List<System.Drawing.PointF> passChipList;
        public List<System.Drawing.PointF> failChipList;
    }
}
