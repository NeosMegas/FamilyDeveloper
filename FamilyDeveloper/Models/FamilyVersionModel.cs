using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace FamilyDeveloper.Models
{
    internal class FamilyVersionModel
    {
        private Document doc;

        private Guid familyVersionParameterGuid = Guid.Parse("85cd0032-c9ee-4cd3-8ffa-b2f1a05328e3");

        public FamilyVersionModel(Document doc)
        {
            this.doc = doc;
        }

        public string GetFamilyVersion()
        {
            if (doc == null || !doc.IsFamilyDocument) return "Неверный документ Revit";
            FamilyParameter p = doc.FamilyManager.get_Parameter(familyVersionParameterGuid);
            if (p == null) return "Параметр версии не найден в семействе";
            return doc.FamilyManager.CurrentType.AsString(p);
        }
    }
}
