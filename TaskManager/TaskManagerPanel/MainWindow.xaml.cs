using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace TaskManager.TaskManagerPanel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
                // Получаем размеры экрана
                Rectangle screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;

                // Создаем Bitmap для сохранения скриншота
                using (Bitmap screenshot = new Bitmap(screenBounds.Width, screenBounds.Height))
                {
                    // Создаем объект Graphics из скриншота
                    using (Graphics graphics = Graphics.FromImage(screenshot))
                    {
                        // Заполняем Bitmap содержимым экрана
                        graphics.CopyFromScreen(screenBounds.Location, System.Drawing.Point.Empty, screenBounds.Size);
                    }

                    // Создаем BitmapImage из скриншота
                    BitmapImage bitmapImage = ConvertBitmapToBitmapImage(screenshot);

                    // Добавляем BitmapImage в список скриншотов
                    screenshots.Add(bitmapImage);

                    // Получаем FlowDocumentReader внутри Grid
                    var flowDocumentReader = (FlowDocumentReader)ReaderView.Children[0];

                    // Создаем новый FlowDocument
                    FlowDocument flowDocument = new FlowDocument();

                    // Создаем новый Paragraph с изображением и описанием
                    Paragraph paragraph = new Paragraph();

                    // Добавляем изображение
                    System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                    image.Source = bitmapImage;
                    paragraph.Inlines.Add(image);

                    // Добавляем описание
                    paragraph.Inlines.Add(new Run("Your description goes here"));

                    // Добавляем Paragraph в FlowDocument
                    flowDocument.Blocks.Add(paragraph);

                    // Устанавливаем FlowDocument в качестве содержимого FlowDocumentReader
                    flowDocumentReader.Document = flowDocument;
                }
            }
            catch (Exception ex)
            {

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
