using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaskManager.Handlers;
using TaskManager.Helpers;
using static TaskManager.Helpers.CaptureWindow;


namespace TaskManager.TaskManagerPanel
{

    public partial class MainWindow : Page, IDockablePaneProvider
    {
        private List<ViewModel> screenshots = new List<ViewModel>();
        private ExternalCommandData _commandData;
        private ExternalEvent _openModelEvent;
        private OpenModelEventHandler _openModelHandler;

        public MainWindow(ViewModel vm, ExternalCommandData commandData)
        {
            InitializeComponent();
            DataContext = vm;
            _commandData = commandData;

            _openModelHandler = new OpenModelEventHandler();
            _openModelEvent = ExternalEvent.Create(_openModelHandler);
        }

        public void UpdateCommandData(ExternalCommandData commandData)
        {
            _commandData = commandData;
        }

        private void PrintScreen(object sender, RoutedEventArgs e)
        {
            try
            {
                IntPtr revitHandle = Process.GetCurrentProcess().MainWindowHandle;
                CaptureWindow captureWindow = new CaptureWindow(revitHandle);
                if (captureWindow.ShowDialog() == true)
                {
                    double[] tempX = { captureWindow.StartPoint.X, captureWindow.EndPoint.X };
                    double[] tempY = { captureWindow.StartPoint.Y, captureWindow.EndPoint.Y };
                    Array.Sort(tempX);
                    Array.Sort(tempY);

                    var startPoint = new System.Windows.Point(tempX[0], tempY[0]);
                    var endPoint = new System.Windows.Point(tempX[1], tempY[1]);

                    int width = (int)Math.Abs(endPoint.X - startPoint.X);
                    int height = (int)Math.Abs(endPoint.Y - startPoint.Y);

                    RECT revitWindowRect;
                    if (GetWindowRect(revitHandle, out revitWindowRect))
                    {
                        int revitLeft = revitWindowRect.Left;
                        int revitTop = revitWindowRect.Top;

                        startPoint.X += revitLeft;
                        startPoint.Y += revitTop;
                        endPoint.X = startPoint.X + width;
                        endPoint.Y = startPoint.Y + height;

                        Bitmap screenshot = new Bitmap(width, height);
                        using (Graphics graphics = Graphics.FromImage(screenshot))
                        {
                            graphics.CopyFromScreen((int)startPoint.X, (int)startPoint.Y, 0, 0, new System.Drawing.Size(width, height));
                        }

                        RenderTargetBitmap inkCanvasRenderBitmap = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Default);
                        var visual = new DrawingVisual();
                        using (var context = visual.RenderOpen())
                        {
                            var brush = new VisualBrush(captureWindow.inkCanvas);
                            context.DrawRectangle(brush, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(width, height)));
                        }
                        inkCanvasRenderBitmap.Render(visual);

                        Bitmap inkCanvasBitmap = BitmapFromBitmapSource(inkCanvasRenderBitmap);
                        BitmapImage inkCanvasImage = ConvertBitmapToBitmapImage(inkCanvasBitmap);

                        // Combine screenshot with InkCanvas
                        Bitmap combinedBitmap = new Bitmap(width, height);
                        using (Graphics g = Graphics.FromImage(combinedBitmap))
                        {
                            g.DrawImage(screenshot, 0, 0);
                            g.DrawImage(inkCanvasBitmap, 0, 0);
                        }
                        BitmapImage bitmapImage = ConvertBitmapToBitmapImage(combinedBitmap);

                        string description = DiscriptionTextBox.Text;

                        var screenshotInfo = new ViewModel
                        {
                            Image = bitmapImage,
                            Description = description,
                            StartPoint = startPoint,
                            EndPoint = endPoint,
                            Coordinates = GetCoordinatesFromRevit(startPoint),
                            Scale = GetCurrentScale(),
                            InkCanvasImage = inkCanvasImage
                        };
                        screenshots.Add(screenshotInfo);

                        Paragraph paragraph = new Paragraph();
                        System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                        image.Source = bitmapImage;
                        image.Margin = new Thickness(5);
                        paragraph.Inlines.Add(image);

                        //System.Windows.Controls.Image image2 = new System.Windows.Controls.Image();
                        //image2.Source = inkCanvasImage;
                        //image2.Margin = new Thickness(5);
                        //paragraph.Inlines.Add(image2);

                        image.MouseLeftButtonDown += (s, args) => ShowCanvasWithImage(screenshotInfo);
                        //image2.MouseLeftButtonDown += (s, args) => OpenModelAtCoordinates(screenshotInfo);

                        paragraph.Inlines.Add(new Run(description));

                        FlowDocument flowDocument = FlowDocReader.Document as FlowDocument;
                        if (flowDocument == null)
                        {
                            flowDocument = new FlowDocument();
                            FlowDocReader.Document = flowDocument;
                        }
                        flowDocument.Blocks.Add(paragraph);

                        DiscriptionTextBox.Clear();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось получить размеры окна Revit.");
                    }
                }
                else
                {
                    MessageBox.Show("Захват области был отменен.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }

        private Bitmap BitmapFromBitmapSource(BitmapSource bitmapSource)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(stream);
                stream.Position = 0;
                return new Bitmap(stream);
            }
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

        private double GetCurrentScale()
        {
            if (_commandData == null)
            {
                return 1.0;
            }

            Document doc = _commandData.Application.ActiveUIDocument.Document;
            Autodesk.Revit.DB.View activeView = doc.ActiveView;

            if (activeView.ViewType == ViewType.ThreeD)
            {
                return 1.0;
            }

            Parameter scaleParameter = activeView.get_Parameter(BuiltInParameter.VIEW_SCALE);
            if (scaleParameter != null && scaleParameter.HasValue)
            {
                string scaleAsString = scaleParameter.AsValueString();
                if (double.TryParse(scaleAsString, out double scaleValue))
                {
                    return scaleValue;
                }
            }
            return 1.0;
        }


        private void OpenModelAtCoordinates(ViewModel screenshotInfo)
        {
            if (_commandData == null)
            {
                MessageBox.Show("Ошибка в CommandData. Вероятнее всего он равен null");
                return;
            }

            _openModelHandler.SetParameters(screenshotInfo, _commandData);
            _openModelEvent.Raise();
        }

        

        private XYZ GetCoordinatesFromRevit(System.Windows.Point point)
        {
            return new XYZ(point.X, point.Y, 0);
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
                Opacity = 0.8 // Можно настроить прозрачность окна
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

            // Добавляем обработчик событий для закрытия окна по клику
            overlayWindow.MouseLeftButtonDown += (s, e) => overlayWindow.Close();

            overlayWindow.Show();
        }


        //------------------------

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
