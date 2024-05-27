using Autodesk.Revit.UI;
using System;
using TaskManager.Commands;
using TaskManager.TaskManagerPanel;

namespace TaskManager
{
    internal class App : IExternalApplication
    {
        internal static DockablePaneId TaskManagerPanel = new DockablePaneId(Guid.Parse("7048e7ac-999d-4368-9454-f66d9e4c6b0b"));
        private static MainWindow _mainWindow;

        public Result OnStartup(UIControlledApplication a)
        {
            var TaskManagerPanelViewModel = new ViewModel();
            _mainWindow = new MainWindow(TaskManagerPanelViewModel, null);
            a.RegisterDockablePane(TaskManagerPanel, "Task Manager Pane", _mainWindow);

            // Создание панели и добавление кнопки
            RibbonPanel panel = a.CreateRibbonPanel("Task Manager");
            PushButtonData buttonData = new PushButtonData("OpenTaskManager", "Open Task Manager", typeof(App).Assembly.Location, "TaskManager.TaskManagerPanel.Command");
            panel.AddItem(buttonData);

            //RibbonPanel panel = application.CreateRibbonPanel("Coordinates Tools");
            PushButtonData buttonGetCoordinates = new PushButtonData(
                "GetCoordinates",
                "Get Coordinates",
                typeof(GetElementCoordinatesCommand).Assembly.Location,
                "TaskManager.Commands.GetElementCoordinatesCommand");

            panel.AddItem(buttonGetCoordinates);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }

        public static void UpdateMainWindowCommandData(ExternalCommandData commandData)
        {
            _mainWindow.UpdateCommandData(commandData);
        }
    }
}
