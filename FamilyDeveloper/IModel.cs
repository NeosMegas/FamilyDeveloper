using Autodesk.Revit.ApplicationServices;
using SimplePluginLogger;

namespace FamilyDeveloper
{
    internal interface IModel
    {
        abstract Application app { get; set; }
        abstract Logger logger { get; set; }
    }
}
