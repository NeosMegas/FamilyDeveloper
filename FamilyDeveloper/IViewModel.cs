using Autodesk.Revit.UI;
using SimplePluginLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyDeveloper
{
    internal interface IViewModel
    {
        abstract UIApplication uiApp { get; set; }
        abstract Logger logger { get; set; }
    }
}
