using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FamilyDeveloper.ViewModels;

namespace FamilyDeveloper.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class AddLookupTableCommand : IExternalCommand
    {
        public UIApplication uiApp { get; set; }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Application app = uiApp.Application;
            Document doc = uiDoc.Document;

            AddReplaceLookupTableViewModel viewModel = new AddReplaceLookupTableViewModel(uiApp, App.logger);
            viewModel.AddLookupTable();

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ReplaceLookupTableCommand : IExternalCommand
    {
        public UIApplication uiApp { get; set; }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Application app = uiApp.Application;
            Document doc = uiDoc.Document;

            AddReplaceLookupTableViewModel viewModel = new AddReplaceLookupTableViewModel(uiApp, App.logger);
            viewModel.ReplaceLookupTable();

            return Result.Succeeded;
        }
    }

}
