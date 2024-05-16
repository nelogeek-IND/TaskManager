using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace TaskManager.Helpers
{
    public class CaptureWindow: Window
    {
        public Point StartPoint { get; private set; }
        public Point EndPoint { get; private set; }
        public double Scale { get; private set; }

        private InkCanvas inkCanvas;
        private bool isDrawing;
        private Point currentPoint;

        public CaptureWindow()
        {
            inkCanvas = new InkCanvas();
            inkCanvas.Background = Brushes.Transparent;
            inkCanvas.DefaultDrawingAttributes.Color = Colors.Red;
            inkCanvas.DefaultDrawingAttributes.Height = 5;
            inkCanvas.DefaultDrawingAttributes.Width = 5;
            this.Content = inkCanvas;

            inkCanvas.MouseMove += InkCanvas_MouseMove;
            inkCanvas.MouseDown += InkCanvas_MouseDown;
            inkCanvas.MouseUp += InkCanvas_MouseUp;
        }

        private void InkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                currentPoint = e.GetPosition(inkCanvas);
                DrawSelectionRectangle(StartPoint, currentPoint);
            }
        }

        private void InkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                StartPoint = e.GetPosition(inkCanvas);
                isDrawing = true;
            }
        }

        private void InkCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing)
            {
                EndPoint = e.GetPosition(inkCanvas);
                isDrawing = false;

                // Здесь можно вычислить масштаб
                Scale = Math.Sqrt(Math.Pow(EndPoint.X - StartPoint.X, 2) + Math.Pow(EndPoint.Y - StartPoint.Y, 2));
                this.Close();
            }
        }

        private void DrawSelectionRectangle(Point startPoint, Point endPoint)
        {
            inkCanvas.Strokes.Clear();

            StylusPointCollection points = new StylusPointCollection
            {
                new StylusPoint(startPoint.X, startPoint.Y),
                new StylusPoint(endPoint.X, startPoint.Y),
                new StylusPoint(endPoint.X, endPoint.Y),
                new StylusPoint(startPoint.X, endPoint.Y),
                new StylusPoint(startPoint.X, startPoint.Y)
            };

            Stroke selectionRectangle = new Stroke(points)
            {
                DrawingAttributes = new DrawingAttributes
                {
                    Color = Colors.Blue,
                    Height = 1,
                    Width = 1
                }
            };

            inkCanvas.Strokes.Add(selectionRectangle);
        }

    }
}
