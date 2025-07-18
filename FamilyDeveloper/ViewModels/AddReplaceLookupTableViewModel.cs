using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FamilyDeveloper.Helpers;
using FamilyDeveloper.Models;
using FamilyDeveloper.Views;
using Microsoft.Win32;
using SimplePluginLogger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FamilyDeveloper.ViewModels
{
    internal class AddReplaceLookupTableViewModel : INotifyPropertyChanged, IViewModel
    {
        /// <summary>
        /// Выполнить команду для всех открытых семейств
        /// </summary>
        private bool forAllOpenedFamilies = false;
        public bool ForAllOpenedFamilies
        {
            get { return forAllOpenedFamilies; }
            set
            {
                forAllOpenedFamilies = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Создавать параметр, содержащий имя таблицы поиска при добавлении новой таблицы?
        /// </summary>
        private bool createLtParameter = true;
        public bool CreateLtParameter
        {
            get { return createLtParameter; }
            set { createLtParameter = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Список всех встроеных групп параметров
        /// </summary>
        public ObservableCollection<BuiltInParameterGroup> parameterGroups;
        public ObservableCollection<BuiltInParameterGroup> ParameterGroups
        {
            get { return parameterGroups; }
        }
        /// <summary>
        /// Выбранная группа параметров для параметра, содержащего имя таблицы поиска
        /// </summary>
        private BuiltInParameterGroup selectedParameterGroup = BuiltInParameterGroup.INVALID;
        public BuiltInParameterGroup SelectedParameterGroup
        {
            get { return selectedParameterGroup; }
            set { selectedParameterGroup = value; OnPropertyChanged(); }
        }


        /// <summary>
        /// Имя параметра, содержащего имя таблицы поиска
        /// </summary>
        private string ltParameterName = "LT_{LT}";
        public string LtParameterName
        {
            get { return ltParameterName; }
            set { ltParameterName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// В случае, если при добавлении новой таблицы, таблица с таким именем уже существует, если true, она будет заменена
        /// </summary>
        private bool replaceIfExist = false;
        public bool ReplaceIfExist
        {
            get { return replaceIfExist; }
            set { replaceIfExist = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// В случае, если при добавлении новой таблицы, таблица с таким именем уже существует, если true, она будет заменена
        /// </summary>
        private bool createIfNotExist = false;
        public bool CreateIfNotExist
        {
            get { return createIfNotExist; }
            set { createIfNotExist = value; OnPropertyChanged(); }
        }


        /// <summary>
        /// Список путей к файлам таблиц поиска, разделённых переносом строки
        /// </summary>
        private string filePathsString;
        public string FilePathsString
        {
            get { return filePathsString; }
            set
            {
                filePathsString = value;
                OnPropertyChanged();
            }
        }
        private string[] FilePaths {
            get {
                return filePathsString.Replace("\r\n", "\n").Split(['\n'], StringSplitOptions.RemoveEmptyEntries);
            }}
        public UIApplication uiApp { get; set; }
        public Logger logger { get; set; }
        private AddReplaceLookupTableModel model;
        private AddReplaceLookupTableWindow view = new AddReplaceLookupTableWindow();
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        #region GetFileNames
        private OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "*.csv|*.csv",
            Multiselect = true,
            Title = "Добавить файл таблицы поиска",
            CheckPathExists = true,
            CheckFileExists = true
        };
        private RelayCommand getFileNamesCommand;
        public RelayCommand GetFileNamesCommand
        {
            get
            {
                return getFileNamesCommand ??= new RelayCommand(o =>
                {
                    if (openFileDialog.ShowDialog() == true)
                    {
                        FilePathsString = string.Join("\n", openFileDialog.FileNames);
                    }
                }
                    );
            }
        }
        #endregion

        public AddReplaceLookupTableViewModel(UIApplication uiApp, Logger logger)
        {
            view.DataContext = this;
            model = new AddReplaceLookupTableModel(uiApp.Application, logger);
            this.uiApp = uiApp;
            this.logger = logger;
            Document nearestFamilyDocument;
            foreach (Document doc in uiApp.Application.Documents)
                if (doc.IsFamilyDocument)
                    nearestFamilyDocument = doc;
            parameterGroups = new ObservableCollection<BuiltInParameterGroup>(FamilyDeveloper.Helpers.ParameterUtils.GetAllBuiltInGroups(uiApp.ActiveUIDocument.Document));
        }

        public void AddLookupTable()
        {
            view.Title = "Добавить таблицу поиска";
            view.cbCreateIfNotExist.Visibility = System.Windows.Visibility.Collapsed;
            if (view.ShowDialog() == true) {
                (int, int) result = (0, 0);
                if (forAllOpenedFamilies)
                {
                    foreach (Document doc in uiApp.Application.Documents)
                    {
                        foreach (string s in FilePaths)
                        {
                            result.Item1 += model.AddLookupTable(doc, s, createLtParameter, ltParameterName, selectedParameterGroup, replaceIfExist) ? 1 : 0;
                            result.Item2++;
                        }
                    }
                }
                else
                {
                    foreach (string s in FilePaths)
                    {
                        result.Item1 += model.AddLookupTable(uiApp.ActiveUIDocument.Document, s, createLtParameter, ltParameterName, selectedParameterGroup, replaceIfExist) ? 1 : 0;
                        result.Item2++;
                    }
                }
                logger.Log($"AddLookupTable: таблиц поиска добавлено {result.Item1}/{result.Item2}");

                TaskDialog td = new TaskDialog("AddLookupTable");
                td.CommonButtons = TaskDialogCommonButtons.Ok;
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Открыть журнал");
                td.MainInstruction = $"Таблиц поиска добавлено:\n{result.Item1}/{result.Item2}";
                if (td.Show() == TaskDialogResult.CommandLink1)
                    logger.OpenLogFile();

            }
        }

        public void ReplaceLookupTable()
        {
            view.Title = "Заменить таблицу поиска";
            view.cbReplaceIfExist.Visibility = System.Windows.Visibility.Collapsed;
            if (view.ShowDialog() == true)
            {
                (int, int) result = (0, 0);
                if (forAllOpenedFamilies)
                {
                    foreach (Document doc in uiApp.Application.Documents)
                    {
                        foreach (string s in FilePaths)
                        {
                            result.Item1 += model.ReplaceLookupTable(doc, s, createLtParameter, ltParameterName, selectedParameterGroup, createIfNotExist) ? 1 : 0;
                            result.Item2++;
                        }
                    }
                }
                else
                {
                    foreach (string s in FilePaths)
                    {
                        result.Item1 += model.ReplaceLookupTable(uiApp.ActiveUIDocument.Document, s, createLtParameter, ltParameterName, selectedParameterGroup, createIfNotExist) ? 1 : 0;
                        result.Item2++;
                    }
                }
                logger.Log($"ReplaceLookupTable: таблиц поиска заменено {result.Item1}/{result.Item2}");

                TaskDialog td = new TaskDialog("ReplaceLookupTable");
                td.CommonButtons = TaskDialogCommonButtons.Ok;
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Открыть журнал");
                td.MainInstruction = $"Таблиц поиска заменено:\n{result.Item1}/{result.Item2}";
                if (td.Show() == TaskDialogResult.CommandLink1)
                    logger.OpenLogFile();
            }
        }
    }
}
