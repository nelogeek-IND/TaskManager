using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;

namespace TaskManager.Helpers
{
    public partial class CaptureWindow : Window
    {
        private Point startPoint;
        private Rectangle selectionRectangle;
        private IntPtr revitHandle;
        public InkCanvas inkCanvas;

        public Point StartPoint { get; private set; }
        public Point EndPoint { get; private set; }

        public CaptureWindow(IntPtr revitHandle)
        {
            InitializeComponent();

            this.revitHandle = revitHandle;

            selectionRectangle = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };


            inkCanvas = new InkCanvas
            {
                Visibility = Visibility.Collapsed,
                Background = Brushes.Transparent, 
                
                DefaultDrawingAttributes = new DrawingAttributes
                {
                    Color = Colors.Red,
                    Height = 5,
                    Width = 5,
                    FitToCurve = true
                }
            };

            DrawingCanvas.Children.Add(selectionRectangle);
            DrawingCanvas.Children.Add(inkCanvas);
            


            // Получаем размеры окна Revit
            revitHandle = Process.GetCurrentProcess().MainWindowHandle;
            RECT revitWindowRect;
            if (GetWindowRect(revitHandle, out revitWindowRect))
            {
                // Позиционируем окно захвата относительно окна Revit
                this.Left = revitWindowRect.Left;
                this.Top = revitWindowRect.Top;
                this.Width = revitWindowRect.Right - revitWindowRect.Left;
                this.Height = revitWindowRect.Bottom - revitWindowRect.Top;
            }
        }

        private bool isSelectingArea = true; // Флаг для отслеживания процесса выделения области
        private bool isDrawing = false; // Флаг для отслеживания процесса рисования

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isSelectingArea || isDrawing)
                return;

            // Получаем смещение окна Revit
            Point revitWindowOffset = GetRevitWindowOffset();

            startPoint = e.GetPosition(this);
            startPoint.X -= revitWindowOffset.X;
            startPoint.Y -= revitWindowOffset.Y;

            Canvas.SetLeft(selectionRectangle, startPoint.X);
            Canvas.SetTop(selectionRectangle, startPoint.Y);
            selectionRectangle.Width = 0;
            selectionRectangle.Height = 0;
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!isSelectingArea || isDrawing)
                return;

            // Получаем смещение окна Revit
            Point revitWindowOffset = GetRevitWindowOffset();

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this);
                currentPoint.X -= revitWindowOffset.X;
                currentPoint.Y -= revitWindowOffset.Y;

                double x = Math.Min(currentPoint.X, startPoint.X);
                double y = Math.Min(currentPoint.Y, startPoint.Y);
                double width = Math.Abs(currentPoint.X - startPoint.X);
                double height = Math.Abs(currentPoint.Y - startPoint.Y);
                Canvas.SetLeft(selectionRectangle, x);
                Canvas.SetTop(selectionRectangle, y);
                selectionRectangle.Width = width;
                selectionRectangle.Height = height;
            }
        }

        // Метод для получения смещения окна Revit
        private Point GetRevitWindowOffset()
        {
            RECT revitWindowRect;
            GetWindowRect(revitHandle, out revitWindowRect);

            Point appWindowPosition = new Point(Left, Top);
            Point revitWindowPosition = new Point(revitWindowRect.Left, revitWindowRect.Top);

            return new Point(appWindowPosition.X - revitWindowPosition.X, appWindowPosition.Y - revitWindowPosition.Y);
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isSelectingArea || isDrawing)
                return;

            EndPoint = e.GetPosition(this);
            StartPoint = startPoint;
        }
        
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && isSelectingArea)
            {
                // Завершаем выделение области и переключаемся в режим рисования
                isSelectingArea = false;
                isDrawing = true;

                // Устанавливаем размеры и позицию InkCanvas по рамке выделения
                double x = Math.Min(StartPoint.X, EndPoint.X);
                double y = Math.Min(StartPoint.Y, EndPoint.Y);
                double width = Math.Abs(EndPoint.X - StartPoint.X);
                double height = Math.Abs(EndPoint.Y - StartPoint.Y);

                Canvas.SetLeft(inkCanvas, x);
                Canvas.SetTop(inkCanvas, y);
                inkCanvas.Width = width;
                inkCanvas.Height = height;
                inkCanvas.Visibility = Visibility.Visible;

                // Скрываем рамку выделения
                //selectionRectangle.Visibility = Visibility.Collapsed;
            }
            else if (e.Key == Key.Enter && isDrawing)
            {
                // Сохраняем скриншот и закрываем окно
                DialogResult = true;
                Close();
            }
            else if (e.Key == Key.Escape)
            {
                // Отменяем и закрываем окно
                DialogResult = false;
                Close();
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
    }
}
