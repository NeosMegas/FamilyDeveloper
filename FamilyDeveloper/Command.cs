#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FamilyDeveloper.ViewModels;
using FamilyDeveloper.Views;
#endregion

namespace FamilyDeveloper
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

            MainViewModel viewModel = new MainViewModel(doc);
            MainWindow view = new MainWindow(viewModel);
            view.Show();

            return Result.Succeeded;
        }
    }
}
