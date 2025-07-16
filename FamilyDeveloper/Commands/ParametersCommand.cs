#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FamilyDeveloper.ViewModels;
#endregion

namespace FamilyDeveloper.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ParametersCommand : IExternalCommand
    {
        public UIApplication uiApp { get; set; }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Application app = uiApp.Application;
            Document doc = uiDoc.Document;

            ParametersViewModel viewModel = new ParametersViewModel(uiApp, App.logger);
            viewModel.AddParametersWithFormulas();
            

            return Result.Succeeded;
        }
    }
}
