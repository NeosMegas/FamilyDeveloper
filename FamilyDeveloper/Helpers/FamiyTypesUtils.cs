using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SimplePluginLogger;

namespace FamilyDeveloper.Helpers
{
    internal static class FamiyTypesUtils
    {
        public static bool IsThereAtLeastOneFamilyType(Document doc, Logger logger, string defaultTypeName = "default")
        {
            if (doc == null || !doc.IsFamilyDocument) return false;
            if (doc.FamilyManager.Types.Size == 0)
            {
                logger.Log($"Документ \"{doc.Title}\" не содержит ни одного типа. Попытка создания типа по умолчанию с именем \"{defaultTypeName}\"");
                using (Transaction t = new Transaction(doc, "Add default family type"))
                {
                    FamilyType ft;
                    try
                    {
                        t.Start();
                        ft = doc.FamilyManager.NewType(defaultTypeName);
                        t.Commit();
                        if (ft != null)
                        {
                            logger.Log($"- ok.");
                            return true;
                        }
                        else
                        {
                            logger.Log($"- неудачно.");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (t != null && t.GetStatus() == TransactionStatus.Started)
                            t.RollBack();
                        TaskDialog.Show(doc.Title, ex.ToString());
                    }
                    return false;
                }
            }
            else
                return true;
        }
    }
}
