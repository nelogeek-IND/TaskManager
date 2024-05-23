using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TaskManager.Models
{
    public class ScreenshotInfo
    {

        public BitmapImage Image { get; set; }
        public string Description { get; set; }
        public System.Windows.Point StartPoint { get; set; }
        public System.Windows.Point EndPoint { get; set; }
        public XYZ Coordinates { get; set; }
        public double Scale { get; set; }
        public BitmapImage InkCanvasImage { get; set; }
    }
}
