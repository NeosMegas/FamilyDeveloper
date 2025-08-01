﻿using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FamilyDeveloper.Views.Converters
{
    internal class BuiltInParameterGroupToTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BuiltInParameterGroup parameterGroup = (BuiltInParameterGroup)value;
            return LabelUtils.GetLabelFor(parameterGroup);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
