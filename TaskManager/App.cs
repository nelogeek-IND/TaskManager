using Autodesk.Revit.UI;
using System;
using TaskManager.TaskManagerPanel;

namespace TaskManager
{
    internal class App : IExternalApplication
    {
        internal static DockablePaneId TaskManagerPanel = new DockablePaneId(Guid.Parse("7048e7ac-999d-4368-9454-f66d9e4c6b0b"));
        
        public Result OnStartup(UIControlledApplication a)
        {
            var TaskManagerPanelViewModel = new ViewModel();
            a.RegisterDockablePane(TaskManagerPanel, "Task Manager Pane", new MainWindow(TaskManagerPanelViewModel, null));

            // Создание панели и добавление кнопки
            RibbonPanel panel = a.CreateRibbonPanel("Task Manager");
            PushButtonData buttonData = new PushButtonData("OpenTaskManager", "Open Task Manager", typeof(App).Assembly.Location, "TaskManager.TaskManagerPanel.Command");
            panel.AddItem(buttonData);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}
