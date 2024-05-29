using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace TaskManager.TaskManagerPanel
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    
        public BitmapImage Image { get; set; }
        public string Description { get; set; }
        public System.Windows.Point StartPoint { get; set; }
        public System.Windows.Point EndPoint { get; set; }
        public XYZ RevitStartPointCoordinates { get; set; }
        public XYZ RevitEndPointCoordinates { get; set; }
        public BitmapImage InkCanvasImage { get; set; }
        public System.Windows.Point RevitWindowCoordinates { get; set; }
        public ElementId ViewId { get; set; }
        public XYZ CenterPoint { get; set; }
        public double Scale { get; set; }
        public double Zoom { get; set; }
        public XYZ ViewCenter { get; set; }
    }
}
