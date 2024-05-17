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

namespace TaskManager.Helpers
{
    public partial class CaptureWindow : Window
    {
        private Point startPoint;
        private Rectangle selectionRectangle;
        private InkCanvas inkCanvas;
        private IntPtr revitHandle;

        public Point StartPoint { get; private set; }
        public Point EndPoint { get; private set; }

        public CaptureWindow(IntPtr revitHandle)
        {
            InitializeComponent();

            this.revitHandle = revitHandle;

            selectionRectangle = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };

            inkCanvas = new InkCanvas
            {
                Background = Brushes.Transparent,
                DefaultDrawingAttributes = new DrawingAttributes
                {
                    Color = Colors.Red,
                    Height = 5,
                    Width = 5,
                    FitToCurve = true
                }
            };

            DrawingCanvas.Children.Add(inkCanvas);
            DrawingCanvas.Children.Add(selectionRectangle);


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

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(this);
            Canvas.SetLeft(selectionRectangle, startPoint.X);
            Canvas.SetTop(selectionRectangle, startPoint.Y);
            selectionRectangle.Width = 0;
            selectionRectangle.Height = 0;
            inkCanvas.Strokes.Clear();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this);
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

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            EndPoint = e.GetPosition(this);
            StartPoint = startPoint;
            DialogResult = true;
            Close();
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
