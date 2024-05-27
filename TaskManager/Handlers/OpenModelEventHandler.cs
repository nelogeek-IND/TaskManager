using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using TaskManager.TaskManagerPanel;

namespace TaskManager.Handlers
{
    public class OpenModelEventHandler : IExternalEventHandler
    {
        private ViewModel _screenshotInfo;
        private ExternalCommandData _commandData;

        public void SetParameters(ViewModel screenshotInfo, ExternalCommandData commandData)
        {
            _screenshotInfo = screenshotInfo;
            _commandData = commandData;
        }

        public void Execute(UIApplication app)
        {
            try
            {
                if (_screenshotInfo == null || _screenshotInfo.InkCanvasImage == null)
                {
                    throw new InvalidOperationException("Скриншот информации или изображение холста равны null.");
                }

                if (_commandData == null)
                {
                    throw new InvalidOperationException("ExternalCommandData равен null.");
                }

                UIDocument uidoc = _commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                using (Transaction tx = new Transaction(doc, "Insert InkCanvas Image"))
                {
                    tx.Start();

                    BitmapImage inkCanvasImage = _screenshotInfo.InkCanvasImage;
                    System.Drawing.Bitmap bitmap = BitmapFromBitmapImage(inkCanvasImage);

                    string tempDir = @"C:\Temp";
                    if (!Directory.Exists(tempDir))
                    {
                        Directory.CreateDirectory(tempDir);
                    }

                    string tempImagePath = Path.Combine(tempDir, "RevScreenshot.png");

                    // Проверка и удаление существующего файла
                    if (File.Exists(tempImagePath))
                    {
                        File.Delete(tempImagePath);
                    }

                    // Сохранение изображения и проверка успешности
                    try
                    {
                        bitmap.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    catch (Exception saveEx)
                    {
                        throw new InvalidOperationException($"Ошибка при сохранении изображения: {saveEx.Message}");
                    }

                    // Инициализация параметров изображения
                    ImageTypeOptions options = new ImageTypeOptions(tempImagePath, false, ImageTypeSource.Import);
                    ImageType imageType = ImageType.Create(doc, options);

                    // Используем указанные координаты для вставки изображения
                    XYZ insertionPoint = new XYZ(1.050088299, 1.546548767, 0.1);
                    View view = uidoc.ActiveView;

                    // Использование ImagePlacementOptions для указания точки вставки
                    ImagePlacementOptions placementOptions = new ImagePlacementOptions(insertionPoint, BoxPlacement.Center);
                    ImageInstance imageInstance = ImageInstance.Create(doc, view, imageType.Id, placementOptions);

                    ElementId imageId = imageInstance.Id;

                    tx.Commit();

                    // Перезагрузка вида для обновления порядка отображения
                    uidoc.RefreshActiveView();
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Произошла ошибка: {ex.Message}");
            }
        }

        //public void Execute(UIApplication app)
        //{
        //    try
        //    {
        //        if (_screenshotInfo == null || _screenshotInfo.InkCanvasImage == null)
        //        {
        //            throw new InvalidOperationException("Скриншот информации или изображение холста равны null.");
        //        }

        //        if (_commandData == null)
        //        {
        //            throw new InvalidOperationException("ExternalCommandData равен null.");
        //        }

        //        UIDocument uidoc = _commandData.Application.ActiveUIDocument;
        //        Document doc = uidoc.Document;

        //        using (Transaction tx = new Transaction(doc, "Insert InkCanvas Image"))
        //        {
        //            tx.Start();

        //            BitmapImage inkCanvasImage = _screenshotInfo.InkCanvasImage;
        //            System.Drawing.Bitmap bitmap = BitmapFromBitmapImage(inkCanvasImage);

        //            string tempDir = @"C:\Temp";
        //            if (!Directory.Exists(tempDir))
        //            {
        //                Directory.CreateDirectory(tempDir);
        //            }

        //            string tempImagePath = Path.Combine(tempDir, "RevScreenshot.png");

        //            // Проверка и удаление существующего файла
        //            if (File.Exists(tempImagePath))
        //            {
        //                File.Delete(tempImagePath);
        //            }

        //            // Сохранение изображения и проверка успешности
        //            try
        //            {
        //                bitmap.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.Png);
        //            }
        //            catch (Exception saveEx)
        //            {
        //                throw new InvalidOperationException($"Ошибка при сохранении изображения: {saveEx.Message}");
        //            }

        //            // Инициализация параметров изображения
        //            ImageTypeOptions options = new ImageTypeOptions(tempImagePath, false, ImageTypeSource.Import);
        //            ImageType imageType = ImageType.Create(doc, options);

        //            XYZ insertionPoint = _screenshotInfo.Coordinates;
        //            View view = uidoc.ActiveView;

        //            // Использование ImagePlacementOptions для указания точки вставки
        //            ImagePlacementOptions placementOptions = new ImagePlacementOptions(insertionPoint, BoxPlacement.Center);
        //            ImageInstance imageInstance = ImageInstance.Create(doc, view, imageType.Id, placementOptions);

        //            tx.Commit();

        //            // Удаление временного файла
        //            File.Delete(tempImagePath);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TaskDialog.Show("Ошибка", $"Произошла ошибка: {ex.Message}");
        //    }
        //}



        public string GetName()
        {
            return "OpenModelEventHandler";
        }

        private System.Drawing.Bitmap BitmapFromBitmapImage(BitmapImage bitmapImage)
        {
            if (bitmapImage == null)
            {
                throw new ArgumentNullException(nameof(bitmapImage), "BitmapImage не должен быть null.");
            }

            // Убедитесь, что BitmapImage загружен и готов к использованию
            bitmapImage.Freeze();

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new PngBitmapEncoder(); // Используем PngBitmapEncoder вместо BmpBitmapEncoder
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                outStream.Seek(0, SeekOrigin.Begin);

                return new System.Drawing.Bitmap(outStream);
            }
        }

    }
}
