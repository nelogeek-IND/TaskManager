using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace TaskManager.TaskManagerPanel
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                DockablePaneId paneId = App.TaskManagerPanel;
                DockablePane pane = commandData.Application.GetDockablePane(paneId);

                // Обновление commandData в MainWindow
                App.UpdateMainWindowCommandData(commandData);

                if (pane.IsShown())
                {
                    pane.Hide();
                }
                else
                {
                    pane.Show();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
