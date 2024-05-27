using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class GetElementCoordinatesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = uidoc.ActiveView;

            // Получение всех элементов на текущем виде
            FilteredElementCollector collector = new FilteredElementCollector(doc, view.Id);
            foreach (Element element in collector)
            {
                Location location = element.Location;
                if (location is LocationPoint locationPoint)
                {
                    XYZ point = locationPoint.Point;
                    TaskDialog.Show("Element Coordinates", $"Element ID: {element.Id}\nCoordinates: {point}");
                }
                else if (location is LocationCurve locationCurve)
                {
                    Curve curve = locationCurve.Curve;
                    XYZ startPoint = curve.GetEndPoint(0);
                    XYZ endPoint = curve.GetEndPoint(1);
                    TaskDialog.Show("Element Coordinates", $"Element ID: {element.Id}\nStart Point: {startPoint}\nEnd Point: {endPoint}");
                }
            }

            return Result.Succeeded;
        }
    }
}
