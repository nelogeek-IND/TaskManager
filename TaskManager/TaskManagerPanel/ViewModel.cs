using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using TaskManager.Helpers;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.TaskManagerPanel
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        //public FlowDocument flowdocument { get; set; }

        private readonly ScreenshotService _screenshotService;
        private readonly Process _process;
        private readonly FlowDocumentReader _flowDocReader;
        private readonly TextBox _descriptionTextBox;


        public ViewModel(ScreenshotService screenshotService, Process process, FlowDocumentReader flowDocReader, TextBox descriptionTextBox)
        {
            _screenshotService = screenshotService;
            _process = process;
            _flowDocReader = flowDocReader;
            _descriptionTextBox = descriptionTextBox;
        }


        public void PrintScreen()
        {
            try
            {
                var captureWindow = new CaptureWindow(_process.MainWindowHandle);
                var screenshotInfo = _screenshotService.CaptureScreenshot(_process, captureWindow);
                screenshotInfo.Description = _descriptionTextBox.Text;

                AddScreenshotToDocument(screenshotInfo);
                _descriptionTextBox.Clear();
            }
            catch (OperationCanceledException)
            {
                System.Windows.MessageBox.Show("Захват области был отменен.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }

        private void AddScreenshotToDocument(ScreenshotInfo screenshotInfo)
        {
            Paragraph paragraph = new Paragraph();
            System.Windows.Controls.Image image = new System.Windows.Controls.Image
            {
                Source = screenshotInfo.Image,
                Margin = new Thickness(5),
                Tag = screenshotInfo
            };
            image.MouseLeftButtonDown += Image_MouseLeftButtonDown;
            paragraph.Inlines.Add(image);

            paragraph.Inlines.Add(new Run(screenshotInfo.Description));

            FlowDocument flowDocument = _flowDocReader.Document as FlowDocument;
            if (flowDocument == null)
            {
                flowDocument = new FlowDocument();
                _flowDocReader.Document = flowDocument;
            }
            flowDocument.Blocks.Add(paragraph);
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image image = sender as System.Windows.Controls.Image;
            if (image != null && image.Tag is ScreenshotInfo screenshotInfo)
            {
                OpenHierarchyObjectAtCoordinates(screenshotInfo);
            }
        }

        private void OpenHierarchyObjectAtCoordinates(ScreenshotInfo screenshotInfo)
        {
            var hierarchyObject = screenshotInfo.HierarchyObject;
            var coordinates = screenshotInfo.Coordinates;
            MessageBox.Show($"Объект: {hierarchyObject}\nКоординаты XYZ: {coordinates}");
        }
    }
}
