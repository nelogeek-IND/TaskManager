using Autodesk.Revit.DB;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaskManager.Helpers;
using TaskManager.Models;

namespace TaskManager.Services
{
    public class ScreenshotService
    {
        public ScreenshotInfo CaptureScreenshot(Process process, CaptureWindow captureWindow)
        {
            IntPtr revitHandle = process.MainWindowHandle;

            if (captureWindow.ShowDialog() == true)
            {
                var selectedArea = GetSelectedArea(captureWindow);
                var startPoint = selectedArea.Item1;
                var endPoint = selectedArea.Item2;
                var width = selectedArea.Item3;
                var height = selectedArea.Item4;

                var coordinates = GetCoordinatesFromRevit(startPoint);
                var bitmapImage = CreateScreenshot(revitHandle, startPoint, width, height, captureWindow);

                return new ScreenshotInfo
                {
                    Image = bitmapImage,
                    Description = string.Empty, // Описание добавим позже
                    StartPoint = startPoint,
                    EndPoint = endPoint,
                    Coordinates = coordinates,
                    HierarchyObject = GetCurrentHierarchyObject()
                };
            }

            throw new OperationCanceledException("Захват области был отменен.");
        }

        private (System.Windows.Point, System.Windows.Point, int, int) GetSelectedArea(CaptureWindow captureWindow)
        {
            double[] tempX = { captureWindow.StartPoint.X, captureWindow.EndPoint.X };
            double[] tempY = { captureWindow.StartPoint.Y, captureWindow.EndPoint.Y };
            Array.Sort(tempX);
            Array.Sort(tempY);

            var startPoint = new System.Windows.Point(tempX[0], tempY[0]);
            var endPoint = new System.Windows.Point(tempX[1], tempY[1]);
            int width = (int)Math.Abs(endPoint.X - startPoint.X);
            int height = (int)Math.Abs(endPoint.Y - startPoint.Y);

            return (startPoint, endPoint, width, height);
        }

        private BitmapImage CreateScreenshot(IntPtr revitHandle, System.Windows.Point startPoint, int width, int height, CaptureWindow captureWindow)
        {
            RECT revitWindowRect;
            if (GetWindowRect(revitHandle, out revitWindowRect))
            {
                int revitLeft = revitWindowRect.Left;
                int revitTop = revitWindowRect.Top;

                startPoint.X += revitLeft;
                startPoint.Y += revitTop;

                Bitmap screenshot = new Bitmap(width, height);
                using (Graphics graphics = Graphics.FromImage(screenshot))
                {
                    graphics.CopyFromScreen((int)startPoint.X, (int)startPoint.Y, 0, 0, new System.Drawing.Size(width, height));
                }

                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Default);
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    var brush = new VisualBrush(captureWindow.inkCanvas);
                    context.DrawRectangle(brush, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(width, height)));
                }
                renderBitmap.Render(visual);

                Bitmap inkCanvasBitmap = BitmapFromBitmapSource(renderBitmap);

                using (Graphics graphics = Graphics.FromImage(screenshot))
                {
                    graphics.DrawImage(inkCanvasBitmap, 0, 0, width, height);
                }

                return ConvertBitmapToBitmapImage(screenshot);
            }

            throw new InvalidOperationException("Не удалось получить размеры окна Revit.");
        }


        private XYZ GetCoordinatesFromRevit(System.Windows.Point point)
        {
            return new XYZ(point.X, point.Y, 0);
        }

        private object GetCurrentHierarchyObject()
        {
            return new object();
        }

        private Bitmap BitmapFromBitmapSource(BitmapSource bitmapSource)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapSource));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        private BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        // Структура RECT для хранения координат прямоугольника окна
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Импорт функции GetWindowRect из user32.dll для получения размеров окна по дескриптору
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        // Импорт функций для работы с DC (Device Context)
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        private const int SRCCOPY = 0x00CC0020;
    }
}
