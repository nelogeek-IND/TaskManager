using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.TaskManagerPanel;

namespace TaskManager
{
    internal class App : IExternalApplication
    {
        internal static DockablePaneId TaskManagerPanel = new DockablePaneId(Guid.Parse("7048e7ac-999d-4368-9454-f66d9e4c6b0b"));

        public Result OnStartup(UIControlledApplication a)
        {
            var TaskManagerPanelViewModel = new ViewModel();
            //stempsHelpPaneViewModel.flowdocument = CreateFlowDocument();
            a.RegisterDockablePane(TaskManagerPanel, "Task Manager Pane", new MainWindow(TaskManagerPanelViewModel));
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }

       
    }
}
