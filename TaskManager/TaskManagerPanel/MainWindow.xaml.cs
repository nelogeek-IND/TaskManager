using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using TaskManager.Helpers;


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
            // Работает на основном мониторе (на втором не работает)
            try
            {
                // Определяем монитор, на котором открыт Revit
                IntPtr revitHandle = Process.GetCurrentProcess().MainWindowHandle;
                var revitScreen = Screen.FromHandle(revitHandle);

                // Создаем новое окно захвата, ограниченное монитором с Revit
                CaptureWindow captureWindow = new CaptureWindow(revitScreen);
                if (captureWindow.ShowDialog() == true)
                {
                    // Получаем точки выделенной области
                    var startPoint = captureWindow.StartPoint;
                    var endPoint = captureWindow.EndPoint;

                    // Получаем размеры выделенной области
                    int width = (int)Math.Abs(endPoint.X - startPoint.X);
                    int height = (int)Math.Abs(endPoint.Y - startPoint.Y);

                    // Создаем Bitmap для сохранения скриншота
                    using (Bitmap screenshot = new Bitmap(width, height))
                    {
                        using (Graphics graphics = Graphics.FromImage(screenshot))
                        {
                            graphics.CopyFromScreen((int)startPoint.X, (int)startPoint.Y, 0, 0, new System.Drawing.Size(width, height));
                        }

                        BitmapImage bitmapImage = ConvertBitmapToBitmapImage(screenshot);
                        screenshots.Add(bitmapImage);

                        // Создаем новый Image элемент
                        System.Windows.Controls.Image imageControl = new System.Windows.Controls.Image();
                        imageControl.Source = bitmapImage;
                        imageControl.Margin = new Thickness(5);

                        // Добавляем Image элемент в StackPanel под кнопкой
                        ScreenshotsContainer.Children.Add(imageControl);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Захват области был отменен.");
                }


            }
            catch (Exception ex)
            {

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
    }
}
