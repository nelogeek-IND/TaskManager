using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaskManager.Helpers;
using TaskManager.Shared;

namespace TaskManager.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class InsertCanvasWithImageCommand : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            IntPtr revitHandle = Process.GetCurrentProcess().MainWindowHandle;
            CaptureWindow captureWindow = new CaptureWindow(revitHandle);

            if (captureWindow.ShowDialog() == true)
            {
                System.Windows.Point start = captureWindow.StartPoint;
                System.Windows.Point end = captureWindow.EndPoint;

                UIView uiview = null;
                IList<UIView> uiviews = uidoc.GetOpenUIViews();
                foreach (UIView uv in uiviews)
                {
                    if (uv.ViewId.Equals(uidoc.ActiveView.Id))
                    {
                        uiview = uv;
                        break;
                    }
                }

                if (uiview != null)
                {
                    XYZ startRevitPoint = RevitCoordinateConverter.ConvertToRevitCoordinates(start, uiview, revitHandle);
                    XYZ endRevitPoint = RevitCoordinateConverter.ConvertToRevitCoordinates(end, uiview, revitHandle);

                    CreateCanvasWithImage(startRevitPoint, endRevitPoint);
                }
            }

            return Result.Succeeded;
        }

        private void CreateCanvasWithImage(XYZ startPoint, XYZ endPoint)
        {
            double width = Math.Abs(endPoint.X - startPoint.X);
            double height = Math.Abs(endPoint.Y - startPoint.Y);

            Canvas canvas = new Canvas
            {
                Width = width,
                Height = height,
                Background = Brushes.Transparent
            };

            // Добавление изображения в Canvas
            System.Windows.Controls.Image image = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri("C:\\path\\to\\your\\image.png")),
                Width = width,
                Height = height
            };

            Canvas.SetLeft(image, 0);
            Canvas.SetTop(image, 0);
            canvas.Children.Add(image);

            // Отображение Canvas в окне или другом элементе управления
        }
    }
}
