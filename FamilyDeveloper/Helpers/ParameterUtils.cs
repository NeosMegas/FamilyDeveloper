﻿using Autodesk.Revit.DB;
using System.Collections.ObjectModel;

namespace FamilyDeveloper.Helpers
{
    internal static class ParameterUtils
    {
        public static List<BuiltInParameterGroup> GetAllBuiltInGroups(Document doc)
        {
            List<BuiltInParameterGroup> groups = new();
            foreach (BuiltInParameterGroup pg in Enum.GetValues(typeof(BuiltInParameterGroup)).Cast<BuiltInParameterGroup>().ToList())
            {
                if (doc.IsFamilyDocument)
                {
                    if(doc.FamilyManager.IsUserAssignableParameterGroup(pg))
                        groups.Add(pg);
                }
                else
                    groups.Add(pg);
            }
            groups = groups.OrderBy(o => LabelUtils.GetLabelFor(o)).ToList();
            return groups;
        }
    }
}
