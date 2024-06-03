using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TaskManager.Shared
{

    public class RevitCoordinateConverter
    {
        public static XYZ ConvertToRevitCoordinates(System.Windows.Point wpfPoint, UIView uiview, IntPtr revitHandle)
        {            
            // Получаем координаты углов в модели Revit
            IList<XYZ> corners = uiview.GetZoomCorners();
            XYZ corner1 = corners[0];
            XYZ corner2 = corners[1];

            // Получаем размеры окна Revit
            Autodesk.Revit.DB.Rectangle rect = uiview.GetWindowRectangle();
            double revitWidth = rect.Right - rect.Left;
            double revitHeight = rect.Bottom - rect.Top;

            // Преобразуем WPF координаты в экранные
            System.Drawing.Point screenPoint = new System.Drawing.Point((int)wpfPoint.X, (int)wpfPoint.Y);
            ClientToScreen(revitHandle, ref screenPoint);

            // Преобразуем экранные координаты в координаты Revit
            double scaleX = (corner2.X - corner1.X) / revitWidth;
            double scaleY = (corner2.Y - corner1.Y) / revitHeight;

            double revitX = corner1.X + (screenPoint.X - rect.Left) * scaleX;
            double revitY = corner1.Y + (screenPoint.Y - rect.Top) * scaleY;

            return new XYZ(revitX, revitY, 0);
        }

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);
    }

}
