using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using TaskManager.TaskManagerPanel;
using MessageBox = System.Windows.Forms.MessageBox;

namespace TaskManager.Handlers
{
    public class OpenViewEventHandler : IExternalEventHandler
    {
        public ViewModel ScreenshotInfo { get; set; }

        public void Execute(UIApplication app)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Переключаемся на сохраненный вид
                Autodesk.Revit.DB.View view = doc.GetElement(ScreenshotInfo.ViewId) as Autodesk.Revit.DB.View;
                if (view != null)
                {
                    uidoc.ActiveView = view;

                    // Устанавливаем масштаб
                    using (Transaction transaction = new Transaction(doc, "Установка масштаба"))
                    {
                        transaction.Start();
                        view.Scale = (int)ScreenshotInfo.Scale;
                        transaction.Commit();
                    }

                    // Фокусируемся на сохраненной области
                    XYZ minPoint = new XYZ(
                        ScreenshotInfo.RevitStartPointCoordinates.X,
                        ScreenshotInfo.RevitStartPointCoordinates.Y,
                        ScreenshotInfo.RevitStartPointCoordinates.Z
                    );

                    XYZ maxPoint = new XYZ(
                        ScreenshotInfo.RevitEndPointCoordinates.X,
                        ScreenshotInfo.RevitEndPointCoordinates.Y,
                        ScreenshotInfo.RevitEndPointCoordinates.Z
                    );

                    // Используем метод ZoomAndCenterRectangle для масштабирования и центровки области
                    uidoc.GetOpenUIViews().FirstOrDefault(uiView => uiView.ViewId == view.Id)?.ZoomAndCenterRectangle(minPoint, maxPoint);

                    // Показать скриншот как оверлей
                    ShowCanvasWithImage(ScreenshotInfo);
                }
                else
                {
                    MessageBox.Show("Не удалось найти сохраненный вид.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при открытии модели: {ex.Message}");
            }
        }

        public string GetName()
        {
            return "Open View Event Handler";
        }

        private void ShowCanvasWithImage(ViewModel screenshotInfo)
        {
            Window overlayWindow = new Window
            {
                Title = "Canvas Overlay",
                Width = screenshotInfo.Image.PixelWidth,
                Height = screenshotInfo.Image.PixelHeight,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                Opacity = 0.8
            };

            Canvas canvas = new Canvas
            {
                Width = screenshotInfo.Image.PixelWidth,
                Height = screenshotInfo.Image.PixelHeight,
                Background = System.Windows.Media.Brushes.Transparent
            };

            System.Windows.Controls.Image image = new System.Windows.Controls.Image
            {
                Source = screenshotInfo.Image,
                Width = screenshotInfo.Image.PixelWidth,
                Height = screenshotInfo.Image.PixelHeight
            };

            Canvas.SetLeft(image, 0);
            Canvas.SetTop(image, 0);
            canvas.Children.Add(image);

            overlayWindow.Content = canvas;

            // Получаем координаты окна Revit
            IntPtr revitHandle = Process.GetCurrentProcess().MainWindowHandle;
            GetWindowRect(revitHandle, out RECT revitWindowRect);

            // Устанавливаем позицию нового окна относительно координат, где был сделан скриншот
            overlayWindow.Left = revitWindowRect.Left + screenshotInfo.StartPoint.X;
            overlayWindow.Top = revitWindowRect.Top + screenshotInfo.StartPoint.Y;

            overlayWindow.MouseLeftButtonDown += (s, e) => overlayWindow.DragMove();
            overlayWindow.MouseRightButtonDown += (s, e) => overlayWindow.Close();

            overlayWindow.Show();
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
    }
}
