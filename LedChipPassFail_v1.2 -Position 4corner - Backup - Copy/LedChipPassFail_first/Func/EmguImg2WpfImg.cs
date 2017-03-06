using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.UI;
using Emgu.CV.Util;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LedChipPassFail_first.Func
{
    public static class BitmapSrcConvert
    {
        [DllImport( "gdi32" )]
        private static extern int DeleteObject( IntPtr o );

        public static BitmapSource ToBitmapSource( IImage image )
        {
            using ( System.Drawing.Bitmap source = image.Bitmap )
            {
                IntPtr ptr = source.GetHbitmap();

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                ptr,
                IntPtr.Zero,
                Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject( ptr );
                return bs;
            }
        }

        

    }
}
