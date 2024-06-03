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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaskManager.Handlers;
using TaskManager.Helpers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using static TaskManager.Helpers.CaptureWindow;
using static TaskManager.TaskManagerPanel.MainWindow;
using MessageBox = System.Windows.MessageBox;
using Transform = Autodesk.Revit.DB.Transform;


namespace TaskManager.TaskManagerPanel
{

    public partial class MainWindow : System.Windows.Controls.Page, IDockablePaneProvider
    {
        private List<Task> tasks = new List<Task>();
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

            UserNameTextBlock.Text = "Имя пользователя";
            UserAvatar = null;
        }

        public void UpdateCommandData(ExternalCommandData commandData)
        {
            _commandData = commandData;
        }


        private void AddTask(object sender, RoutedEventArgs e)
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

                        string description = DescriptionTextBox.Text;

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


                        // Создаем новый объект Task и добавляем его в коллекцию задач
                        var newTask = new Task
                        {
                            TaskName = $"Task #{tasks.Count + 1}",
                            CreatorName = "Текущий пользователь", // Заменить на имя текущего пользователя
                            CreationDate = DateTime.Now.ToString("dd.MM.yyyy"),
                            Description = description,
                            Screenshot = bitmapImage,
                            InkImage = inkCanvasImage,
                            StartPointImg = startPoint,
                            EndtPointImg = endPoint,
                            RevitStartPointCoordinates = lowerLeft,
                            RevitEndPointCoordinates = upperRight,
                            ViewId = viewId,
                            Scale = GetCurrentScale(),
                        };

                        tasks.Add(newTask);  // Добавляем задачу в локальный список

                        ViewModel viewModel = DataContext as ViewModel;
                        if (viewModel != null)
                        {
                            viewModel.Tasks.Add(newTask);
                        }
                        else
                        {
                            MessageBox.Show("Не удалось получить ViewModel.");
                        }

                        DescriptionTextBox.Clear();
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

        private void OpenModelInRevit(Task task)
        {
            if (_commandData == null)
            {
                MessageBox.Show("Ошибка в CommandData. Вероятнее всего он равен null");
                return;
            }

            openViewHandler.task = task;
            openViewEvent.Raise();
        }

        private void ViewScreenshot(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;



            // Открывает скрин в окне
            //BitmapImage screenshot = button.Tag as BitmapImage;

            //if (screenshot != null)
            //{
            //    System.Windows.Window window = new System.Windows.Window
            //    {
            //        Title = "Скриншот",
            //        Content = new System.Windows.Controls.Image { Source = screenshot, Stretch = Stretch.Uniform },
            //        Width = screenshot.Width,
            //        Height = screenshot.Height,
            //    };
            //    window.Show();
            //}

            Task task = button?.DataContext as Task;
            if (task != null)
            {
                OpenModelInRevit(task);
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
