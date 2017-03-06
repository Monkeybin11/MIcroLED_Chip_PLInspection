using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.Util;
using Emgu.CV.Util;

namespace LedChipPassFail_first.Data
{
    public class ImgPResult
    {
        public int ChipPassCount = 0;
        public int ChipFailCount = 0;
        //public int ChipFailCount { get { return ChipBigCount + ChipSmallCount; } }
        //public int ChipBigCount;
        //public int ChipSmallCount;
        public List<ExResult> OutData = new System.Collections.Generic.List<ExResult>();
        public List<int> SizeHist = new System.Collections.Generic.List<int>();
        public List<int> ChipIntensityHist = new System.Collections.Generic.List<int>();
        
    }
    public class ContourData
    {
        public VectorOfPoint Coordinate;
        public double Area;
        public double[] Center;
    }

    public class ExResult
    {
        public int Hindex;
        public int Windex;
        public bool PassFail;
        public double Intensity;
        public double ContourSize;
        public ExResult(int hindex,int windex,bool passfail,double inten, double contsize)
        {
            Hindex = hindex;
            Windex = windex;
            PassFail = passfail;
            Intensity    = inten;
            ContourSize = contsize;
        }
    }

}
