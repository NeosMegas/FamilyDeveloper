using Autodesk.Revit.UI;
using SimplePluginLogger;

namespace FamilyDeveloper
{
    internal interface IViewModel
    {
        abstract UIApplication uiApp { get; set; }
        abstract Logger logger { get; set; }
    }
}
