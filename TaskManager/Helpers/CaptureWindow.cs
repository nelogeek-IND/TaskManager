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
        public CaptureWindow()
        {
            // Создаем InkCanvas для рисования
            InkCanvas inkCanvas = new InkCanvas();
            inkCanvas.Background = Brushes.Transparent;
            inkCanvas.DefaultDrawingAttributes.Color = Colors.Red;
            inkCanvas.DefaultDrawingAttributes.Height = 5;
            inkCanvas.DefaultDrawingAttributes.Width = 5;

            // Добавляем обработчики событий мыши для рисования
            inkCanvas.MouseMove += InkCanvas_MouseMove;
            inkCanvas.MouseDown += InkCanvas_MouseDown;
            inkCanvas.MouseUp += InkCanvas_MouseUp;

            // Добавляем InkCanvas на окно
            this.Content = inkCanvas;
        }

        private void InkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Получаем текущие координаты мыши и рисуем линию
            InkCanvas inkCanvas = sender as InkCanvas;
            if (inkCanvas != null && e.LeftButton == MouseButtonState.Pressed)
            {
                StylusPointCollection points = new StylusPointCollection();
                points.Add(e.StylusDevice.GetStylusPoints(inkCanvas));
                inkCanvas.Strokes.Add(new Stroke(points));
            }
        }

        private void InkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Начинаем рисование при нажатии на кнопку мыши
            InkCanvas inkCanvas = sender as InkCanvas;
            if (inkCanvas != null)
            {
                inkCanvas.CaptureMouse();
            }
        }

        private void InkCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Завершаем рисование при отпускании кнопки мыши
            InkCanvas inkCanvas = sender as InkCanvas;
            if (inkCanvas != null)
            {
                inkCanvas.ReleaseMouseCapture();
            }
        }

    }
}
