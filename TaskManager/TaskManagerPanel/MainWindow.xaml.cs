using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaskManager.Handlers;
using TaskManager.Helpers;
using static TaskManager.Helpers.CaptureWindow;
using static TaskManager.TaskManagerPanel.MainWindow;
using MessageBox = System.Windows.MessageBox;
using Transform = Autodesk.Revit.DB.Transform;


namespace TaskManager.TaskManagerPanel
{

    public partial class MainWindow : Page, IDockablePaneProvider
    {
        private List<ViewModel> screenshots = new List<ViewModel>();
        private ExternalCommandData _commandData;

        private ExternalEvent openViewEvent;
        private OpenViewEventHandler openViewHandler;

        public MainWindow(ViewModel vm, ExternalCommandData commandData)
        {
            InitializeComponent();
            DataContext = vm;
            _commandData = commandData;

            openViewHandler = new OpenViewEventHandler();
            openViewEvent = ExternalEvent.Create(openViewHandler);
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

                        // Запоминаем координаты окна Revit относительно экрана
                        var revitStartPointCoordinates = new System.Windows.Point(revitWindowRect.Left, revitWindowRect.Top);
                        var revitEndPointCoordinates = new System.Windows.Point(revitWindowRect.Right, revitWindowRect.Bottom);

                        UIDocument uidoc = _commandData.Application.ActiveUIDocument;

                        Autodesk.Revit.DB.ViewPlan view = uidoc.ActiveGraphicalView as ViewPlan;
                        if (view == null)
                        {
                            MessageBox.Show("Активный вид не является планом этажа.");
                            return;
                        }

                        UIView uiView = uidoc.GetOpenUIViews().FirstOrDefault(v => v.ViewId == view.Id);
                        if (uiView == null)
                        {
                            MessageBox.Show("Не удалось получить UIView для текущего вида.");
                            return;
                        }

                        var corners = uiView.GetZoomCorners();
                        XYZ lowerLeft = corners[0];
                        XYZ upperRight = corners[1];

                        ElementId viewId = view.Id;  // Получаем идентификатор активного вида

                        XYZ viewCenter = view.Origin;  // Центр вида

                        double zoomLevel = GetZoomLevel(view);

                        var screenshotInfo = new ViewModel
                        {
                            Image = bitmapImage,
                            Description = description,
                            StartPoint = startPoint,
                            EndPoint = endPoint,
                            RevitStartPointCoordinates = lowerLeft,
                            RevitEndPointCoordinates = upperRight,
                            Scale = GetCurrentScale(),
                            InkCanvasImage = inkCanvasImage,
                            ViewId = viewId,
                            CenterPoint = viewCenter,
                            Zoom = zoomLevel,
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

                        image.MouseLeftButtonDown += (s, args) => OpenModelAtCoordinates(screenshotInfo); // ShowCanvasWithImage(screenshotInfo);
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

        private double GetZoomLevel(ViewPlan view)
        {
            // Проверка, что переданный вид - это план этажа
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            UIDocument uidoc = _commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Получаем размеры окна Revit
            Autodesk.Revit.DB.Rectangle revitRect = uidoc.GetOpenUIViews().FirstOrDefault(uiv => uiv.ViewId == view.Id)?.GetWindowRectangle();
            if (revitRect == null)
                throw new InvalidOperationException("Не удалось получить размеры окна Revit для текущего вида.");

            // Преобразуем Autodesk.Revit.DB.Rectangle в System.Drawing.Rectangle
            System.Drawing.Rectangle viewRect = new System.Drawing.Rectangle(revitRect.Left, revitRect.Top, revitRect.Right - revitRect.Left, revitRect.Bottom - revitRect.Top);

            double windowWidth = viewRect.Width;
            double windowHeight = viewRect.Height;

            // Получаем границы вида
            BoundingBoxXYZ boundingBox = view.get_BoundingBox(null);
            double viewWidth = boundingBox.Max.X - boundingBox.Min.X;
            double viewHeight = boundingBox.Max.Y - boundingBox.Min.Y;

            // Получаем масштаб аннотаций и текущий масштаб вида
            double annotationScale = view.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_METRIC).AsDouble();
            double viewScale = view.Scale;

            // Рассчитываем текущий уровень зума как отношение размеров окна к размерам вида
            double zoomLevelWidth = windowWidth / (viewWidth * viewScale / annotationScale);
            double zoomLevelHeight = windowHeight / (viewHeight * viewScale / annotationScale);

            // Возвращаем минимальное значение, чтобы соответствовать обоим осям
            return Math.Min(zoomLevelWidth, zoomLevelHeight);
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

            openViewHandler.ScreenshotInfo = screenshotInfo;
            openViewEvent.Raise();
        }


        private void ShowModelCoordinates(object sender, RoutedEventArgs e)
        {
            try
            {
                UIDocument uidoc = _commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Выбираем точку на экране
                XYZ pickedPoint = uidoc.Selection.PickPoint("Выберите точку для получения координат");

                // Получаем элемент, ближайший к этой точке
                Reference pickedReference = uidoc.Selection.PickObject(ObjectType.Element, "Выберите объект для получения координат");
                Element element = doc.GetElement(pickedReference);

                // Получаем местоположение элемента
                Location location = element.Location;
                XYZ locationPoint = null;

                if (location is LocationPoint locationPointElement)
                {
                    locationPoint = locationPointElement.Point;
                }
                else if (location is LocationCurve locationCurveElement)
                {
                    // Получаем среднюю точку кривой как местоположение объекта
                    locationPoint = locationCurveElement.Curve.Evaluate(0.5, true);
                }

                if (locationPoint != null)
                {
                    MessageBox.Show($"Координаты объекта: X = {locationPoint.X}, Y = {locationPoint.Y}, Z = {locationPoint.Z}");
                }
                else
                {
                    MessageBox.Show("Не удалось определить координаты объекта.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }


        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        const uint MOUSEEVENTF_LEFTUP = 0x04;

        private void SimulateClick(System.Windows.Point screenPoint)
        {
            // Установить курсор в позицию
            SetCursorPos((int)screenPoint.X, (int)screenPoint.Y);

            // Симулировать нажатие и отпускание левой кнопки мыши
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
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
