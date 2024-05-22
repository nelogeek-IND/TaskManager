using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using TaskManager.Services;
using TaskManager.TaskManagerPanel;

namespace TaskManager
{
    internal class App : IExternalApplication
    {
        internal static DockablePaneId TaskManagerPanel = new DockablePaneId(Guid.Parse("7048e7ac-999d-4368-9454-f66d9e4c6b0b"));

        public Result OnStartup(UIControlledApplication a)
        {
            var screenshotService = new ScreenshotService();
            var process = Process.GetCurrentProcess(); 
            var flowDocumentReader = new FlowDocumentReader(); 
            var descriptionTextBox = new System.Windows.Controls.TextBox(); 

            var TaskManagerPanelViewModel = new ViewModel(screenshotService, process, flowDocumentReader, descriptionTextBox);

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
