using System.Windows;
using System.Windows.Media.Imaging;

namespace TaskManager.Models
{
    public class ScreenshotInfo
    {
        public BitmapImage Image { get; set; }
        public string Description { get; set; }
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public Autodesk.Revit.DB.XYZ Coordinates { get; set; }
        public object HierarchyObject { get; set; }
    }
}
