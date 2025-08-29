using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using FamilyDeveloper.Helpers;
using SimplePluginLogger;
using System.IO;

namespace FamilyDeveloper.Models
{
    internal class ParametersModel : IModel
    {
        public Application app { get; set; }
        public Logger logger {  get; set; }

        public ParametersModel(Application app, Logger logger)
        {
            this.app = app;
            this.logger = logger;
        }

        /// <summary>
        /// Создаёт, изменят или удаляет параметры в соответствии с заданной строкой
        /// создания/изменения/удаления параметров <paramref name="parametersString"/>
        /// </summary>
        /// <param name="doc">Документ, в котором производятся изменения</param>
        /// <param name="parametersString">Строка создания/изменения/удаления параметров</param>
        /// <returns>Количество успешно изменённых параметров, общее количество параметров, планируемых для изменения</returns>
        public (int, int) AddParametersWithFormulas(Document doc, string parametersString)
        {
            if (!doc.IsFamilyDocument)
            {
                logger.Log($"AddParametersWithFormulas: Документ \"{doc.Title}\" не является семейством.");
                return (0, 0);
            }
            if (!FamiyTypesUtils.IsThereAtLeastOneFamilyType(doc, logger))
            {
                logger.Log($"AddLookupTable: Документ \"{doc.Title}\" не содержит ни одного типа.");
                return (0, 0);
            }
            logger.Log(doc.Title);
            int totalParameters = 0;
            int parametersProcessed = 0;
            int rowNumber = 0;
            string[] lines = parametersString.Replace("\r\n", "\n").Split(['\n'], StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, bool> parameterSetFormulas = [];
            Dictionary<string, string> parameterFormulas = [];
            Dictionary<string, string> parameterNewNames = [];

#if R19 || R20 || R21 || R22
            Dictionary<string, ParameterType> parameterTypes = [];
#elif R23 || R24 || R25 || R26
            Dictionary<string, ForgeTypeId> parameterTypes = [];
#endif
            Dictionary<string, bool> parameterIsInstance = [];
#if R19 || R20 || R21 || R22
            Dictionary<string, BuiltInParameterGroup> parameterGroups = [];
#elif R23 || R24 || R25 || R26
            Dictionary<string, ForgeTypeId> parameterGroups = [];
#endif
            Dictionary<string, BuiltInCategory> parameterCategories = [];
            Dictionary<string, bool> parameterSetValues = [];
            Dictionary<string, string> parameterValues = [];
            Dictionary<string, bool> parameterDelete = [];
            Dictionary<string, bool> parameterIsShared = [];
            Dictionary<string, string> parameterSpfFileName = [];
            foreach (string str in lines)
            {
                rowNumber++;
                if (str.Trim().Substring(0, 2) == "//") continue;
                string[] strSplit = str.Split(["=="], StringSplitOptions.None);
                string name = "";
                string newName = "";
#if R19 || R20 || R21 || R22
                BuiltInParameterGroup group = BuiltInParameterGroup.INVALID;
                ParameterType type = ParameterType.Invalid;
#elif R23 || R24 || R25 || R26
                ForgeTypeId group = null;
                ForgeTypeId type = null;
#endif
                BuiltInCategory category = BuiltInCategory.INVALID;
                bool isInstance = false;
                string isInstanceString = "";
                bool setFormula = str.Contains("==");
                string formula = null;
                bool setValue = false;
                string valueString = "";
                string[] firstPart = strSplit[0].Trim().Split([';'], StringSplitOptions.RemoveEmptyEntries);
                bool delete = false;
                bool isShared = false;
                string spfFileName = "";
                if (strSplit.Length == 1 || strSplit.Length == 2)
                {
                    name = firstPart[0].Trim();
                    if (name.Contains(">"))
                    {
                        string[] names = name.Split(['>'], StringSplitOptions.RemoveEmptyEntries);
                        if (names.Length == 2)
                        {
                            name = names[0].Trim();
                            newName = names[1].Trim();
                        }
                    }
                    if (name.Contains("["))
                    {
                        if (newName != "")
                        {
                            logger.Log($"AddParametersWithFormulas: (строка {rowNumber}): Невозможно переименовать общий параметр.");
                        }
                        isShared = true;
                        if (!name.Contains("]"))
                        {
                            logger.Log($"AddParametersWithFormulas: (строка {rowNumber}): Синтаксическая ошибка, отсутствует \"]\"");
                        }
                        string[] filename = name.Split(['['], StringSplitOptions.RemoveEmptyEntries);
                        if (filename.Length != 2)
                        {
                            logger.Log($"AddParametersWithFormulas: (строка {rowNumber}): Ошибка в имени параметра или пути файла общих параметров.");
                        }
                        name = filename[0];
                        spfFileName = filename[1].Substring(0, filename[1].Length - 1);
                        if (!File.Exists(spfFileName))
                        {
                            logger.Log($"AddParametersWithFormulas: (строка {rowNumber}): Файл \"{spfFileName}\" не найден.");
                        }
                    }
                    if (name.Substring(0, 1) == "~")
                    {
                        delete = true;
                        name = name.Substring(1);
                    }
                    if (firstPart.Length == 3 || firstPart.Length == 4 || firstPart.Length == 5)
                    {
                        if (isShared)
                        {
                            isInstanceString = firstPart[1].Trim();
                            isInstance = isInstanceString == "i" || isInstanceString == "inst" || isInstanceString == "istance" || isInstanceString == "1";
                            group =
#if R19 || R20 || R21 || R22
                                GetParameterGroupByName(firstPart[2].Trim());
#elif R23 || R24 || R25 || R26
                                ParameterUtils.GetParameterGroupTypeId(
                                GetParameterGroupByName(firstPart[2].Trim()));
#endif
                            if (firstPart.Length == 4)
                            {
                                setValue = true;
                                valueString = firstPart[3];
                            }
                        }
                        else
                        {
                            if (firstPart.Length == 4 || firstPart.Length == 5)
                            {
                                string typeString = firstPart[1].Trim();
                                string[] typeStringPart = typeString.Split(['('], StringSplitOptions.RemoveEmptyEntries);
                                if (typeStringPart.Length > 1)
                                {
                                    type = GetParameterTypeByName(typeStringPart[0].Trim());
                                    typeStringPart[1] = typeStringPart[1].Trim(['(', ')']);
                                    category = GetBuiltInCategoryByName(typeStringPart[1].Trim());
                                }
                                else
                                    type = GetParameterTypeByName(firstPart[1].Trim());
                                isInstanceString = firstPart[2].Trim();
                                isInstance = isInstanceString == "i" || isInstanceString == "inst" || isInstanceString == "istance" || isInstanceString == "1";
                                group =
#if R19 || R20 || R21 || R22
                                    GetParameterGroupByName(firstPart[3].Trim());
#elif R23 || R24 || R25 || R26
                                    ParameterUtils.GetParameterGroupTypeId(
                                    GetParameterGroupByName(firstPart[3].Trim()));
#endif
                                if (firstPart.Length == 5)
                                {
                                    setValue = true;
                                    valueString = firstPart[4];
                                }
                            }
                        }
                    }
                    else if (firstPart.Length == 1 || firstPart.Length == 2)
                    {
                        FamilyParameter p = doc.FamilyManager.get_Parameter(name);
                        if (p != null)
                        {
                            type = p.Definition
#if R19 || R20 || R21 || R22
                                .ParameterType;
#elif R23 || R24 || R25 || R26
                                .GetDataType();
#endif
                            isInstance = p.IsInstance;
                            if (firstPart.Length == 2)
                            {
                                isInstanceString = firstPart[1].Trim();
                                if (isInstanceString == "i" || isInstanceString == "inst" || isInstanceString == "istance" || isInstanceString == "1")
                                    isInstance = true;
                                else if (isInstanceString == "t" || isInstanceString == "type" || isInstanceString == "0")
                                    isInstance = false;
                            }
                            group = p.Definition
#if R19 || R20 || R21 || R22
                                .ParameterGroup;
#elif R23 || R24 || R25 || R26
                                .GetGroupTypeId();
#endif
                        }
                        else
                        {
                            logger.Log($"AddParametersWithFormulas: (строка {rowNumber}): Параметр \"{name}\" не найден.");
                        }
                    }
                    if (setFormula)
                    {
                        if (strSplit.Length == 2)
                            formula = strSplit[1].Trim();
                        if (formula == string.Empty)
                            formula = null;
                    }
                    if (!parameterFormulas.ContainsKey(name))
                    {
                        parameterSetFormulas.Add(name, setFormula);
                        parameterFormulas.Add(name, formula);
                        parameterNewNames.Add(name, newName);
                        parameterGroups.Add(name, group);
                        parameterTypes.Add(name, type);
                        parameterCategories.Add(name, category);
                        parameterIsInstance.Add(name, isInstance);
                        parameterSetValues.Add(name, setValue);
                        parameterValues.Add(name, valueString);
                        parameterDelete.Add(name, delete);
                        parameterIsShared.Add(name, isShared);
                        parameterSpfFileName.Add(name, spfFileName);
                    }
                }
                else
                {
                    logger.Log($"AddParametersWithFormulas: (строка {rowNumber}): Неверное количество параметров: strSplit.Length = {strSplit.Length}; firstPart.Length = {(firstPart != null ? firstPart.Length.ToString() : "null")}");
                }
            }
            //rowNumber = 0;
            using (Transaction t = new Transaction(doc, "AddParametersWithFormulas"))
            {
                try
                {
                    t.Start();
                    foreach (KeyValuePair<string, string> e in parameterFormulas)
                    {
                        totalParameters++;
                        bool result;
                        if (parameterDelete[e.Key])
                        {
                            result = DeleteParameter(doc, e.Key);
                            if(result) parametersProcessed++;
                            continue;
                        }
                        if (doc.FamilyManager.get_Parameter(e.Key) != null)
                        {
                            result = ChangeParameterWithFormula(doc, e.Key, parameterNewNames[e.Key], parameterGroups[e.Key], parameterTypes[e.Key], parameterIsInstance[e.Key], parameterSetValues[e.Key], parameterValues[e.Key], parameterSetFormulas[e.Key], e.Value);
                        }
                        else
                        {
                            result = AddParameterWithFormula(doc, e.Key, parameterGroups[e.Key], parameterTypes[e.Key], parameterCategories[e.Key], parameterIsInstance[e.Key], parameterSetValues[e.Key], parameterValues[e.Key], parameterSetFormulas[e.Key], e.Value, parameterIsShared[e.Key], parameterSpfFileName[e.Key]);
                        }
                        if (result) parametersProcessed++;
                    }
                    t.Commit();
                }
                catch (Exception ex)
                {
                    if (t != null && t.GetStatus() == TransactionStatus.Started)
                        t.RollBack();
                    logger.Log(ex.ToString());
                }
            }
            return (parametersProcessed, totalParameters);
        }

        /// <summary>
        /// Создаёт новый параметр семейства
        /// </summary>
        /// <param name="doc">Документ семейства</param>
        /// <param name="name">Имя параметра</param>
        /// <param name="group">Группа параметра</param>
        /// <param name="type">Тип параметра</param>
        /// <param name="category">Категория параметра (для параметров, содержащих тип семейства)</param>
        /// <param name="isInstance">Является ли параметр параметром экземпляра (true) или типа (false)</param>
        /// <param name="setValue">true, если нужно задать значение параметра</param>
        /// <param name="valueString">Значение параметра в формате строки</param>
        /// <param name="setFormula">true, если нужно задать формулу параметра</param>
        /// <param name="formula">Формула параметра</param>
        /// <param name="isShared">Является ли параметр общим (true) или нет (false)</param>
        /// <param name="spfFileName">Путь к файлу общих параметров, если <paramref name="isShared"/> = <see cref="true"/></param>
        /// <returns>Отчёт о создании</returns>
        private bool AddParameterWithFormula(Document doc, string name,
#if R19 || R20 || R21 || R22
            BuiltInParameterGroup group,
            ParameterType type,
#elif R23 || R24 || R25 || R26
            ForgeTypeId group,
            ForgeTypeId type,
#endif
            BuiltInCategory category, bool isInstance, bool setValue, string valueString, bool setFormula, string formula, bool isShared, string spfFileName)
        {
            bool success = false;
            FamilyParameter p = null;
            try
            {
                if (isShared)
                {
                    string tempFileName = app.SharedParametersFilename;
                    try
                    {
                        app.SharedParametersFilename = spfFileName;
                        ExternalDefinition def = GetExternalDefinition(spfFileName, name);
                        if (def != null)
                        {
                            p = doc.FamilyManager.AddParameter(def, group, isInstance);
                            logger.Log($"AddParameterWithFormula: Создан \"{p.Definition.Name}\"");
                            success = true;
                        }
                        else
                            logger.Log($"AddParameterWithFormula: Ошибка получения внешнего определения для \"{name}\" from \"{spfFileName}\"");
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"AddParameterWithFormula: Exception (isShared): {ex}");
                        success = false;
                    }
                    finally
                    {
                        app.SharedParametersFilename = tempFileName;
                    }
                }
                else
                {
                    if (category != BuiltInCategory.INVALID)
                        p = doc.FamilyManager.AddParameter(name, group, Category.GetCategory(doc, category), isInstance);
                    else
                        p = doc.FamilyManager.AddParameter(name, group, type, isInstance);
                    logger.Log($"AddParameterWithFormula: Создан \"{p.Definition.Name}\"");
                }
                if (p != null)
                {
                    success = true;
                    if (setValue)
                    {
                        logger.Log($"AddParameterWithFormula: Попытка задать значение \"{valueString}\"");
                        if (SetParameterValue(doc, p, valueString))
                            logger.Log(" - ok");
                        else
                        {
                            logger.Log(" - неудачно");
                            success = false;
                        }
                    }
                    if (setFormula)
                    {
                        logger.Log($"AddParameterWithFormula: Попытка задать формулу \"{formula}\"");
                        doc.FamilyManager.SetFormula(p, formula);
                        RecalculateParameterFormulas(doc, GetParameterDependentParameters(doc, p.Definition.Name));
                        logger.Log(" - ok");
                    }
                }
                else
                {
                    logger.Log($"AddParameterWithFormula: Не удалось создать \"{name}\"");
                    success = false;
                }
            }
            catch (Exception ex)
            {
                logger.Log($"\nAddParameterWithFormula: Exception: {ex}");
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Изменяет существующий параметр семейства, если это возможно
        /// </summary>
        /// <param name="doc">Документ семейства</param>
        /// <param name="name">Имя изменяемого параметра</param>
        /// <param name="newName">Новое имя парамметра (если переименовывается)</param>
        /// <param name="group">Группа параметра, если нужно её изменить (пока недоступно)</param>
        /// <param name="type">Тип параметра</param>
        /// <param name="isInstance">Является ли параметр параметром экземпляра (true) или типа (false)</param>
        /// <param name="setValue">true, если нужно задавать значение параметра</param>
        /// <param name="valueString">Задаваемое парметру значение в строковом формате</param>
        /// <param name="setFormula">true, если нужно менять формулу параметра</param>
        /// <param name="formula">Формула для параметра</param>
        /// <returns>Отчёт об изменении</returns>
        private bool ChangeParameterWithFormula(Document doc, string name, string newName,
#if R19 || R20 || R21 || R22
            BuiltInParameterGroup group,
            ParameterType type,
#elif R23 || R24 || R25 || R26
            ForgeTypeId group,
            ForgeTypeId type,
#endif
            bool isInstance, bool setValue, string valueString, bool setFormula, string formula)
        {
            bool success = false;
            try
            {
                FamilyParameter p = doc.FamilyManager.get_Parameter(name);
                if (p == null)
                {
                    logger.Log($"ChangeParameterWithFormula: Параметр \"{name}\" не существует.");
                    return false;
                }
                logger.Log($"ChangeParameterWithFormula: Найден \"{p.Definition.Name}\"");
                if (p.Definition
#if R19 || R20 || R21 || R22
                    .ParameterGroup
#elif R23 || R24 || R25 || R26
                    .GetGroupTypeId()
#endif
                    != group)
#if R19 || R20 || R21 || R22
                    logger.Log($"ChangeParameterWithFormula: Невозможно изменить группу с \"{p.Definition.ParameterGroup}\" на \"{group}\"");
#elif R23 || R24 || R25 || R26
                    logger.Log($"ChangeParameterWithFormula: Невозможно изменить группу с \"{LabelUtils.GetLabelForGroup(p.Definition.GetGroupTypeId())}\" на \"{LabelUtils.GetLabelForGroup(group)}\"");
#endif
                if (p.Definition
#if R19 || R20 || R21 || R22
                    .ParameterType
#elif R23 || R24 || R25 || R26
                    .GetDataType()
#endif
                    != type)
#if R19 || R20 || R21 || R22
                    logger.Log($"ChangeParameterWithFormula: невозможно изменить тип с \"{p.Definition.ParameterType}\" на \"{type}\"");
#elif R23 || R24 || R25 || R26
                    logger.Log($"ChangeParameterWithFormula: невозможно изменить тип с \"{LabelUtils.GetLabelForSpec(p.Definition.GetDataType())}\" на \"{LabelUtils.GetLabelForSpec(type)}\"");
#endif
                if (p.IsInstance != isInstance)
                {
                    if (isInstance)
                    {
                        logger.Log("ChangeParameterWithFormula: Изменено на параметр экземпляра.");
                        doc.FamilyManager.MakeInstance(p);
                    }
                    else if (!isInstance)
                    {
                        logger.Log("ChangeParameterWithFormula: Изменено на параметр типа.");
                        doc.FamilyManager.MakeType(p);
                    }
                }

                if (newName != "" && newName != name)
                {
                    logger.Log($"ChangeParameterWithFormula: Попытка переименовать \"{name}\" в \"{newName}\"");
                    doc.FamilyManager.RenameParameter(p, newName);
                    logger.Log(" - ok");
                }
                if (setFormula)
                {
                    logger.Log($"ChangeParameterWithFormula: Попытка задать формулу \"{formula}\"");
                    doc.FamilyManager.SetFormula(p, formula);
                    RecalculateParameterFormulas(doc, GetParameterDependentParameters(doc, p.Definition.Name));
                    logger.Log(" - ok");
                }
                if (setValue)
                {
                    logger.Log($"ChangeParameterWithFormula: Попытка задать значение \"{valueString}\"");
                    if (SetParameterValue(doc, p, valueString))
                        logger.Log(" - ok");
                    else
                        logger.Log(" - неудачно");
                }
                success = true;
            }
            catch (Exception ex)
            {
                logger.Log($"\nChangeParameterWithFormula: Exception: {ex}");
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Удаляет параметр семейства с именем <paramref name="name"/>
        /// </summary>
        /// <param name="doc">Документ семейства</param>
        /// <param name="name">Имя параметра для удаления</param>
        /// <returns>Количество Отчёт об удалении</returns>
        private bool DeleteParameter(Document doc, string name)
        {
            bool success = false;
            try
            {
                FamilyParameter p = doc.FamilyManager.get_Parameter(name);
                if (p == null)
                {
                    logger.Log($"DeleteParameter: Параметр \"{name}\" не существует.");
                    return false;
                }
                bool isShared = p.IsShared;
                SharedParameterElement speToDelete = null;
                if (isShared)
                {
                    FilteredElementCollector fec = new FilteredElementCollector(doc).OfClass(typeof(SharedParameterElement));
                    foreach (SharedParameterElement spe in fec)
                        if (doc.FamilyManager.get_Parameter(spe.Name).Id == p.Id)
                        {
                            speToDelete = spe;
                            break;
                        }
                }
                logger.Log($"DeleteParameter: Найден \"{p.Definition.Name}\"");
                doc.FamilyManager.RemoveParameter(p);
                logger.Log($"DeleteParameter: \"{name}\" удалён.");
                if (isShared && speToDelete != null && speToDelete.IsValidObject)
                {
                    logger.Log($"DeleteParameter: Найдено определение общего параметра с именеи \"{name}\".");
                    Guid speGuid = speToDelete.GuidValue;
                    string speName = speToDelete.Name;
                    doc.Delete(speToDelete.Id);
                    logger.Log($"DeleteParameter: Удалено определение общего параметра \"{speName}\" c GUID {speGuid}.");
                }
                success = true;
            }
            catch (Exception ex)
            {
                logger.Log($"\nDeleteParameter: Exception: {ex}");
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Возвращает категорию <see cref="BuiltInCategory"/> по имени категории
        /// </summary>
        /// <param name="categoryName">Имя встроенной категории (например, OST_GenericModel"</param>
        /// <returns><see cref="BuiltInCategory"/></returns>
        private BuiltInCategory GetBuiltInCategoryByName(string categoryName)
        {
            string[] names = Enum.GetNames(typeof(BuiltInCategory));
            string nameFound = names.FirstOrDefault(s => s == categoryName);
            int i = -1;
            for (int j = 0; j < names.Length; j++)
                if (names[j] == categoryName)
                    i = j;
            BuiltInCategory result = BuiltInCategory.INVALID;
            if (i >= 0) result = (BuiltInCategory)Enum.GetValues(typeof(BuiltInCategory)).GetValue(i);
            return result;
        }

        /// <summary>
        /// Возвращает тип параметра <see cref="ParameterType"/> по имени типа параметра.
        /// </summary>
        /// <param name="typeName">Имя типа параметра (например, Text)</param>
        /// <returns><see cref="ParameterType"/></returns>
        private ParameterType GetParameterTypeByName(string typeName)
        {
            string[] names = Enum.GetNames(typeof(ParameterType));
            string nameFound = names.FirstOrDefault(s => s == typeName);
            int i = -1;
            for (int j = 0; j < names.Length; j++)
                if (names[j] == typeName)
                    i = j;
            ParameterType result = ParameterType.Invalid;
            if (i >= 0) result = (ParameterType)Enum.GetValues(typeof(ParameterType)).GetValue(i);
            return result;
        }

        /// <summary>
        /// Получает группу параметра <see cref="BuiltInParameterGroup"/> по имени группы
        /// </summary>
        /// <param name="groupName">Имя группы</param>
        /// <returns><see cref="BuiltInParameterGroup"/></returns>
        private BuiltInParameterGroup GetParameterGroupByName(string groupName)
        {
            string[] names = Enum.GetNames(typeof(BuiltInParameterGroup));
            string nameFound = names.FirstOrDefault(s => s == groupName);
            int i = -1;
            for (int j = 0; j < names.Length; j++)
                if (names[j] == groupName)
                    i = j;
            BuiltInParameterGroup result = BuiltInParameterGroup.INVALID;
            if (i >= 0) result = (BuiltInParameterGroup)Enum.GetValues(typeof(BuiltInParameterGroup)).GetValue(i);
            return result;
        }

        /// <summary>
        /// Получает <see cref="ExternalDefinition"/> из файла общих параметров <paramref name="spfFileName"/>
        /// </summary>
        /// <param name="spfFileName">Путь к файлу общих параметров</param>
        /// <param name="parameterName">Имя параметра в ФОП</param>
        /// <returns><see cref="ExternalDefinition"/> общего параметра</returns>
        private ExternalDefinition GetExternalDefinition(string spfFileName, string parameterName)
        {
            ExternalDefinition externalDefinition = null;
            try
            {
                DefinitionFile definitionFile = app.OpenSharedParameterFile();
                if (definitionFile == null)
                    externalDefinition = null;
                else
                    foreach (DefinitionGroup definitionGroup in definitionFile.Groups)
                    {
                        foreach (ExternalDefinition d in definitionGroup.Definitions)
                        {
                            if (d.Name == parameterName)
                                externalDefinition = d;
                        }
                    }
            }
            catch (Exception ex)
            {
                logger.Log("GetExternalDefinition: " + ex.ToString());
            }
            return externalDefinition;
        }

        /// <summary>
        /// Задаёт значение параметра семейства, переданное в виде строки
        /// </summary>
        /// <param name="doc">Документ семейства</param>
        /// <param name="p">Параметр семейства</param>
        /// <param name="valueString">Значение параметра в виде строки</param>
        /// <returns>true, если</returns>
        private bool SetParameterValue(Document doc, FamilyParameter p, string valueString)
        {
            switch (p.StorageType)
            {
                case StorageType.Integer:
                    int valueInt;
                    if (int.TryParse(valueString, out valueInt))
                    {
                        doc.FamilyManager.Set(p, valueInt);
                        return true;
                    }
                    return false;
                case StorageType.Double:
                    double valueDouble;
                    if (double.TryParse(valueString, out valueDouble))
                    {
#if R19 || R20
                        doc.FamilyManager.Set(p, UnitUtils.ConvertToInternalUnits(valueDouble, doc.GetUnits().GetFormatOptions(p.Definition.UnitType).DisplayUnits));
#else
                        doc.FamilyManager.Set(p, UnitUtils.ConvertToInternalUnits(valueDouble, doc.GetUnits().GetFormatOptions(p.Definition.GetSpecTypeId()).GetUnitTypeId()));
#endif
                        return true;
                    }
                    return false;
                case StorageType.String:
                    doc.FamilyManager.Set(p, valueString);
                    return true;
                case StorageType.ElementId:
                    ElementId valueElementId = GetElementIdByName(doc, valueString);
                    if (valueElementId != null)
                    {
                        doc.FamilyManager.Set(p, valueElementId);
                        return true;
                    }
                    return false;
            }
            return false;
        }


        /// <summary>
        /// Возвращает ElementId по имени семейства и типа, заданными строкой вида:
        /// "Имя семейства : Имя типа", либо null, если такого не найдено.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="familyNameTypeName"></param>
        /// <returns></returns>
        private ElementId GetElementIdByName(Document doc, string familyNameTypeName)
        {
            string[] names = familyNameTypeName.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (names.Length != 2) return null;
            ParameterValueProvider pvpFamilyName = new ParameterValueProvider(new ElementId(BuiltInParameter.ALL_MODEL_FAMILY_NAME));
            ParameterValueProvider pvpTypeName = new ParameterValueProvider(new ElementId(BuiltInParameter.ALL_MODEL_TYPE_NAME));
            List<ElementFilter> filters =
            [
                new ElementParameterFilter(new FilterStringRule(pvpFamilyName, new FilterStringEquals(), names[0].Trim(), true), false),
                new ElementParameterFilter(new FilterStringRule(pvpTypeName, new FilterStringEquals(), names[1].Trim(), true), false),
            ];
            LogicalAndFilter filter = new LogicalAndFilter(filters);
            FilteredElementCollector fec = new FilteredElementCollector(doc).WhereElementIsElementType().WherePasses(filter);
            return fec.ToElementIds().FirstOrDefault();
        }

        /// <summary>
        /// Вовзращает список параметров семейства, которые зависят от указанного параметра (он содержится в их формулах)
        /// </summary>
        /// <param name="doc">Документ семейства</param>
        /// <param name="parameterName">Имя параметра семейства, для которого нужно найти зависимые параметры семейства</param>
        /// <returns></returns>
        private List<FamilyParameter> GetParameterDependentParameters(Document doc, string parameterName)
        {
            List<FamilyParameter> dependentParameters = new List<FamilyParameter>();
            foreach (FamilyParameter p in doc.FamilyManager.Parameters)
            {
                if (p.IsDeterminedByFormula && p.Formula.Contains(parameterName))
                    dependentParameters.Add(p);
            }
            return dependentParameters;
        }

        /// <summary>
        /// Добавляет к существующей формуле параметра пробел в конце. После этого Revit автоматически его удаляет,
        /// что вызывает обновление и пересчёт формулы.
        /// Может быть в будущем найду более простой вариант пересчёта формул.
        /// </summary>
        /// <param name="doc">Документ семейства, в котором нужно пересчитать формулы</param>
        /// <param name="parameters">Список параметров семейства, для которых нужно пересчитать формулы</param>
        private void RecalculateParameterFormulas(Document doc, List<FamilyParameter> parameters)
        {
            foreach (FamilyParameter p in parameters)
            {
                doc.FamilyManager.SetFormula(p, p.Formula + " ");
            }
        }
    }
}
