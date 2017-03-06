using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace LedChipPassFail_first
{
    public static class TestFileSavePath
    {
        public static string BasePath;
        public static string ThresName;
        public static string ContourName;
        public static string Con_CenterName;
        public static string BoxName;


        public static void Setting( string filepath ) {
            var newDeck = filepath.Split('\\');
            string name = newDeck.Last().Split('.').First();
            newDeck = newDeck.Take( newDeck.Count() - 1 ).ToArray();
            BasePath = Path.Combine( newDeck );
            ThresName       = name + "_Thresed.png";
            ContourName     = name + "_Contour.png";
            Con_CenterName  = name + "_ContxCenter.png";
            BoxName         = name + "_Boxed.png";

            ThresName       = BasePath + "\\" + ThresName       ;
            ContourName     = BasePath + "\\" + ContourName     ;
            Con_CenterName  = BasePath + "\\" + Con_CenterName  ;
            BoxName         = BasePath + "\\" + BoxName         ;
        }

    }
}
