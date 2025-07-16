using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FamilyDeveloper.Models;
using FamilyDeveloper.Views;
using SimplePluginLogger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FamilyDeveloper.ViewModels
{
    internal class ParametersViewModel : INotifyPropertyChanged, IViewModel
    {
        private bool forAllOpenedFamilies;
        public bool ForAllOpenedFamilies {
            get { return forAllOpenedFamilies; }
            set { 
                forAllOpenedFamilies = value;
                OnPropertyChanged();
            }}
        private string parametersString;
        public string ParametersString {
            get { return parametersString; }
            set {
                parametersString = value;
                OnPropertyChanged();
            }
        }
        private ParametersWindow view = new ParametersWindow();
        public ParametersModel model { get; set; }
        public UIApplication uiApp { get; set; }
        public Logger logger { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public ParametersViewModel(UIApplication uiApp, Logger logger)
        {
            view.DataContext = this;
            model = new ParametersModel(uiApp.Application, logger);
            this.uiApp = uiApp;
            this.logger = logger;
            //parametersString = "// Enter parameter names with formulas.\r\n// To create new:\r\n// parameter name;parameter type<i.e. Text>;is instance <i|t|instance|type|1|0>;parameter group;[parameter value] == formula\r\n// parameter name;FamilyType(BuiltInCategory);is instance <i|t|instance|type|1|0>;parameter group;[parameter value] == formula\r\nNewParameter;Text;i;PG_ADSK_MODEL_PROPERTIES;3 == \"1 + 2\"\r\nNewParameter;FamilyType(OST_Entourage);t;PG_IVALID;FamilyName:FamilyType\r\n\r\n// To set formula to existing parameter:\r\n// parameter name == formula\r\nNewParameter == 1 + 2\r\n\r\n// To rename existing parameter (and maybe set formula):\r\n// parameter name > new parameter name == formula\r\nOldParameterName > NewParameterName == 1 + 2\r\n\r\n// To create shared parameter:\r\n// shared parameter name[shared parameter file path];is instance <i|t|instance|type|1|0>;parameter group;[parameter value] == formula\r\nNewSharedParameter[c:\\SPfs\\SPF.txt];i;PG_ADSK_MODEL_PROPERTIES;3 == \"1 + 2\"\r\n\r\n// To delete existing parameter:\r\n// ~existing parameter name\r\n~ExistingParameterName\r\n";
             parametersString = "// Введите имена параметров с формулами.\r\n// Для создания нового параметра:\r\n// Имя параметра;Тип параметра<например Text>;экземепляр <i|t|instance|type|1|0>;группа параметров;значение параметра == формула\r\n// Имя параметра;Типоразмер из семейства(Встроенная категория);экземпляр <i|t|instance|type|1|0>;группа параметров;значение параметра == формула\r\n// Пример создания параметра типа с типом данных \"текст\" в группе параметров \"Строительство\" с заданием значения параметра и формулы\r\nNewParameter;Text;t;PG_CONSTRUCTION;абв == \"абв\"\r\n// Пример создания параметра экземпляра с типом данных \"целое\" в группе параметров \"Свойства модели\" без задания значения параметра и формулы\r\nNewParameter;Integer;i;PG_ADSK_MODEL_PROPERTIES\r\n// Пример создания параметра экземпляра с типом данных \"типоразмер из семейства\" категории Антураж в группе параметров \"Прочее\" с заданием значения параметра\r\nNewParameter;FamilyType(OST_Entourage);t;PG_IVALID;FamilyName:FamilyType\r\n\r\n// Для задания формулы существующему параметру:\r\n// Имя параметра == формула\r\nNewParameter == 1 + 2\r\n\r\n// Для переименования существующего параметра (и, возможно, задания формулы):\r\n// Имя параметра > Новое имя параметра\r\n// Имя параметра > Новое имя параметра == формула\r\nOldParameterName > NewParameterName\r\nOldParameterName > NewParameterName == 1 + 2\r\n\r\n// Для создания общего параметра:\r\n// Имя общего параметра[путь к файлу общих параметров];экземпляр <i|t|instance|type|1|0>;группа параметров;значение параметра == формула\r\n// Пример создания общего параметра экземпляра в группе параметров \"Свойства модели\" с заданием значения параметра и формулы\r\nNewSharedParameter[c:\\SPfs\\SPF.txt];i;PG_ADSK_MODEL_PROPERTIES;3 == 1 + 2\r\n\r\n// Для удаления существующего параметра:\r\n// ~Имя общего параметра\r\n~ExistingParameterName\r\n";
        }

        public void AddParametersWithFormulas()
        {
            if (view.ShowDialog() == true)
            {
                (int, int) result = (0, 0);
                (int, int) totalResult = (0, 0);
                if (ForAllOpenedFamilies)
                {
                    foreach (Document doc in uiApp.Application.Documents)
                    {
                        result = model.AddParametersWithFormulas(doc, ParametersString);
                        totalResult.Item1 += result.Item1;
                        totalResult.Item2 += result.Item2;
                    }
                }
                else
                    totalResult = model.AddParametersWithFormulas(uiApp.ActiveUIDocument.Document, ParametersString);
                TaskDialog td = new TaskDialog("AddParametersWithFormulas");
                td.CommonButtons = TaskDialogCommonButtons.Ok;
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Открыть журнал");
                td.MainInstruction = $"Создано/изменено/удалено параметров:\n{totalResult.Item1}/{totalResult.Item2}";
                if (td.Show() == TaskDialogResult.CommandLink1)
                    logger.OpenLogFile();
            }
        }
    }
}
