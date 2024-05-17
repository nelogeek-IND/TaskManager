using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using TaskManager.Helpers;
using static TaskManager.Helpers.CaptureWindow;


namespace TaskManager.TaskManagerPanel
{

    public partial class MainWindow : Page, IDockablePaneProvider
    {
        private List<BitmapImage> screenshots = new List<BitmapImage>();

        public MainWindow(ViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void PrintScreen(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем дескриптор окна Revit
                IntPtr revitHandle = Process.GetCurrentProcess().MainWindowHandle;

                // Создаем новое окно захвата, ограниченное окном Revit
                CaptureWindow captureWindow = new CaptureWindow(revitHandle);
                if (captureWindow.ShowDialog() == true)
                {
                    // Получаем точки выделенной области
                    var startPoint = captureWindow.StartPoint;
                    var endPoint = captureWindow.EndPoint;

                    // Получаем размеры выделенной области
                    int width = (int)Math.Abs(endPoint.X - startPoint.X);
                    int height = (int)Math.Abs(endPoint.Y - startPoint.Y);

                    // Получаем координаты окна Revit
                    RECT revitWindowRect;
                    if (GetWindowRect(revitHandle, out revitWindowRect))
                    {
                        int revitLeft = revitWindowRect.Left;
                        int revitTop = revitWindowRect.Top;

                        // Корректируем координаты выделенной области относительно окна Revit
                        startPoint.X += revitLeft;
                        startPoint.Y += revitTop;
                        endPoint.X = startPoint.X + width;
                        endPoint.Y = startPoint.Y + height;

                        // Создаем Bitmap для сохранения скриншота
                        using (Bitmap screenshot = new Bitmap(width, height))
                        {
                            using (Graphics graphics = Graphics.FromImage(screenshot))
                            {
                                graphics.CopyFromScreen((int)startPoint.X, (int)startPoint.Y, 0, 0, new System.Drawing.Size(width, height));
                            }

                            BitmapImage bitmapImage = ConvertBitmapToBitmapImage(screenshot);
                            screenshots.Add(bitmapImage);

                            // Создаем новый параграф с изображением
                            Paragraph paragraph = new Paragraph();
                            System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                            image.Source = bitmapImage;
                            //image.Width = 300; // Устанавливаем ширину изображения, если необходимо
                            image.Margin = new Thickness(5);
                            paragraph.Inlines.Add(image);

                            // Добавляем описание к изображению
                            paragraph.Inlines.Add(new Run("Описание вашего скриншота"));

                            // Добавляем параграф в FlowDocument
                            FlowDocument flowDocument = FlowDocReader.Document as FlowDocument;
                            if (flowDocument == null)
                            {
                                flowDocument = new FlowDocument();
                                FlowDocReader.Document = flowDocument;
                            }
                            flowDocument.Blocks.Add(paragraph);
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Не удалось получить размеры окна Revit.");
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Захват области был отменен.");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }

        private Bitmap CaptureScreenArea(XYZ startPoint, XYZ endPoint, double width, double height)
        {
            // Преобразуем точки в координаты экрана
            System.Drawing.Point screenStartPoint = new System.Drawing.Point((int)startPoint.X, (int)startPoint.Y);
            System.Drawing.Point screenEndPoint = new System.Drawing.Point((int)endPoint.X, (int)endPoint.Y);

            // Создаем Bitmap для сохранения скриншота
            Bitmap bitmap = new Bitmap((int)width, (int)height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Заполняем Bitmap содержимым экрана
                graphics.CopyFromScreen(screenStartPoint, System.Drawing.Point.Empty, new System.Drawing.Size((int)width, (int)height));
            }

            return bitmap;
        }



        private BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.VisibleByDefault = false;
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState()
            {
                DockPosition = DockPosition.Right,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
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
