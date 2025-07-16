using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SimplePluginLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyDeveloper.Models
{
    internal class AddReplaceLookupTableModel
    {
        private Application app;
        private Logger logger;
        /// <summary>
        /// Флаг, показывающий, что ReplaceLookupTable вызвано из AddLookupTable или, что
        /// AddLookupTable вызвано из ReplaceLookupTable (для избежания бесконечного зацикливания)
        /// </summary>
        private bool secondMethodCalled = false;

        public AddReplaceLookupTableModel(Application app, Logger logger)
        {
            this.app = app;
            this.logger = logger;
        }

        /// <summary>
        /// Заменяет существующую в семействе таблицу поиска
        /// </summary>
        /// <param name="doc">Документ семейства</param>
        /// <param name="ltFilePath">Путь к файлу таблицы поиска</param>
        /// <param name="createLtParameter">Если true, будет создан параметр, содержащий имя таблицы поиска</param>
        /// <param name="ltParameterName">Имя параметра. Если содержит {LT}, будет заменено, значение будет заменено на имя файла таблицы поиска</param>
        /// <param name="parameterGroup">Группа параметров, в которую будет добавлен параметрв</param>
        /// <param name="createIfNotExist">Если таблица с таким именем не существует, она будет создана, если true</param>
        /// <returns>true в случае успеха, иначе - false</returns>
        public bool ReplaceLookupTable(Document doc, string ltFilePath, bool createLtParameter = true, string ltParameterName = "LT_{LT}", BuiltInParameterGroup parameterGroup = BuiltInParameterGroup.INVALID, bool createIfNotExist = true)
        {
            if (!doc.IsFamilyDocument)
            {
                logger.Log($"ReplaceLookupTable: Документ \"{doc.Title}\" не является семейством.");
                return false;
            }
            if (string.IsNullOrEmpty(ltFilePath) || !File.Exists(ltFilePath))
            {
                logger.Log($"ReplaceLookupTable: Файл \"{ltFilePath}\" не существует.");
                return false;
            }
            if (!secondMethodCalled)
                logger.Log($"ReplaceLookupTable: семейство \"{doc.Title}\"");
            FamilySizeTableManager fstm = FamilySizeTableManager.GetFamilySizeTableManager(doc, new ElementId(BuiltInParameter.RBS_LOOKUP_TABLE_NAME));
            if (fstm == null)
            {
                logger.Log($"ReplaceLookupTable: ошибка обращения к менеджеру таблиц поиска. Возможно, в документ \"{doc.Title}\" ещё не добавлялись таблицы поиска.");
                using (Transaction t1 = new Transaction(doc, "CreateFamilySizeTableManager"))
                {
                    try
                    {
                        t1.Start();
                        if (!FamilySizeTableManager.CreateFamilySizeTableManager(doc, new ElementId(BuiltInParameter.RBS_LOOKUP_TABLE_NAME)))
                        {
                            logger.Log($"ReplaceLookupTable: ошибка создания менеджера таблиц поиска");
                            return false;
                        }
                        else
                            logger.Log("ReplaceLookupTable: создан менеджера таблиц поиска");
                        t1.Commit();
                    }
                    catch (Exception ex1)
                    {
                        if (t1 != null && t1.GetStatus() == TransactionStatus.Started)
                            t1.RollBack();
                        logger.Log($"ReplaceLookupTable(CreateFamilySizeTableManager) exception: {ex1}");
                    }
                }
                fstm = FamilySizeTableManager.GetFamilySizeTableManager(doc, new ElementId(BuiltInParameter.RBS_LOOKUP_TABLE_NAME));
                if (fstm == null)
                {
                    logger.Log($"ReplaceLookupTable: не удалось создать менеджер таблиц поиска.");
                    return false;
                }
            }
            string ltName = Path.GetFileNameWithoutExtension(ltFilePath);
            if (!fstm.HasSizeTable(ltName))
            {
                logger.Log($"ReplaceLookupTable: таблица \"{ltName}\" не существует.");
                if (createIfNotExist && !secondMethodCalled)
                {
                    secondMethodCalled = true;
                    return AddLookupTable(doc, ltFilePath, createLtParameter, ltParameterName, parameterGroup);
                }    
                else
                    return false;
            }
            using (Transaction t = new Transaction(doc, "ReplaceLookupTable \"" + ltName + "\""))
            {
                FamilyParameter currentFp = null;
                string newFormula = "";
                try
                {
                    logger.Log($"ReplaceLookupTable: попытка замены таблицы поиска \"{ltName}\"");
                    t.Start();
                    FamilySizeTableErrorInfo ei = new FamilySizeTableErrorInfo();
                    if (!fstm.ImportSizeTable(doc, ltFilePath, ei))
                    {
                        logger.Log($"ReplaceLookupTable: ошибка импорта таблицы поиска \"{ltName}\": {ei.FamilySizeTableErrorType}");
                        if (t.GetStatus() == TransactionStatus.Started)
                            t.RollBack();
                        return false;
                    }
                    logger.Log("ReplaceLookupTable: замена успешна");
                    List<FamilyParameter> parametersDeterminedByFormula = [];
                    FamilyParameter LT_parameter = null;
                    foreach (FamilyParameter fp in doc.FamilyManager.Parameters)
                    {
                        if (fp.IsDeterminedByFormula)
                        {
                            parametersDeterminedByFormula.Add(fp);
                            if (fp.Formula == $"\"{ltName}\"")
                                LT_parameter = fp;
                        }
                    }
                    if (LT_parameter == null && createLtParameter)
                    {
                        ltParameterName = ltParameterName.Replace("{LT}", ltName);
                        LT_parameter = doc.FamilyManager.AddParameter(ltParameterName, parameterGroup, ParameterType.Text, false);
                        if (LT_parameter != null)
                        {
                            logger.Log($"ReplaceLookupTable: параметр \"{ltParameterName}\" создан. Попытка задать ему значение \"{ltName}\":");
                            doc.FamilyManager.SetFormula(LT_parameter, "\"" + ltName + "\"");
                            logger.Log($"Ok.");
                        }
                        else
                            logger.Log($"ReplaceLookupTable: ошибка создания параметра \"{ltParameterName}\"");
                    }
                    foreach (FamilyParameter fp in parametersDeterminedByFormula)
                    {
                        if ((LT_parameter != null && fp.Formula.Contains("size_lookup") && fp.Formula.Contains(LT_parameter.Definition.Name)) ||
                           (fp.Formula.Contains("size_lookup") && fp.Formula.Contains(ltName)))
                        {
                            currentFp = fp;
                            newFormula = fp.Formula + " ";
                            doc.FamilyManager.SetFormula(fp, fp.Formula + " ");
							// ToDo: update depending on this parameter parameters
                        }
                    }
                    t.Commit();
                }
                catch (Exception ex)
                {
                    if (t != null && t.GetStatus() == TransactionStatus.Started)
                        t.RollBack();
                    logger.Log($"ReplaceLookupTable exception: {currentFp?.Definition.Name}\n{currentFp?.Formula}\n{newFormula}\n\n{ex}");
                }
            }
            return true;
        }

        /// <summary>
        /// Добавляет таблицу поиска
        /// </summary>
        /// <param name="doc">Документ семейства</param>
        /// <param name="ltFilePath">Футь к файлу таблицы поиска</param>
        /// <param name="createLtParameter">Если true, будет создан параметр, содержащий имя таблицы поиска</param>
        /// <param name="ltParameterName">Имя параметра. Если содержит {LT}, будет заменено, значение будет заменено на имя файла таблицы поиска</param>
        /// <param name="parameterGroup">Группа параметров, в которую будет добавлен параметрв</param>
        /// <param name="replaceIfExist">Если таблица с таким именем уже существует, она будет заменена, если true</param>
        /// <returns>true в случае успеха, иначе - false</returns>
        public bool AddLookupTable(Document doc, string ltFilePath, bool createLtParameter = true, string ltParameterName = "LT_{LT}", BuiltInParameterGroup parameterGroup = BuiltInParameterGroup.INVALID, bool replaceIfExist = false)
        {
            if (!doc.IsFamilyDocument)
            {
                logger.Log($"AddLookupTable: Документ \"{doc.Title}\" не является семейством.");
                return false;
            }
            if (string.IsNullOrEmpty(ltFilePath) || !File.Exists(ltFilePath))
            {
                logger.Log($"AddLookupTable: Файл \"{ltFilePath}\" не существует.");
                return false;
            }
            if (!secondMethodCalled)
                logger.Log($"AddLookupTable: семейство \"{doc.Title}\"");
            string ltName = Path.GetFileNameWithoutExtension(ltFilePath);
            FamilyParameter LT_parameter = null;
            using (Transaction t = new Transaction(doc, "AddLookupTable \"" + ltName + "\""))
            {
                try
                {
                    FamilySizeTableManager fstm = FamilySizeTableManager.GetFamilySizeTableManager(doc, new ElementId(BuiltInParameter.RBS_LOOKUP_TABLE_NAME));
                    if (fstm == null)
                    {
                        using (Transaction t1 = new Transaction(doc, "CreateFamilySizeTableManager"))
                        {
                            try
                            {
                                t1.Start();
                                if (!FamilySizeTableManager.CreateFamilySizeTableManager(doc, new ElementId(BuiltInParameter.RBS_LOOKUP_TABLE_NAME)))
                                {
                                    logger.Log($"AddLookupTable: ошибка создания менеджера таблиц поиска");
                                    return false;
                                }
                                else
                                    logger.Log("AddLookupTable: создан менеджера таблиц поиска");
                                t1.Commit();
                            }
                            catch (Exception ex1)
                            {
                                if (t1 != null && t1.GetStatus() == TransactionStatus.Started)
                                    t1.RollBack();
                                logger.Log($"AddLookupTable(CreateFamilySizeTableManager) exception: {ex1}");
                            }
                        }
                        fstm = FamilySizeTableManager.GetFamilySizeTableManager(doc, new ElementId(BuiltInParameter.RBS_LOOKUP_TABLE_NAME));
                        if (fstm == null)
                        {
                            logger.Log($"AddLookupTable: ошибка получения менеджера таблиц поиска.");
                            return false;
                        }
                    }
                    if (fstm.HasSizeTable(ltName))
                    {
                        logger.Log($"AddLookupTable: таблица поиска {ltName} уже существует.");
                        if (replaceIfExist && !secondMethodCalled)
                        {
                            secondMethodCalled = true;
                            return ReplaceLookupTable(doc, ltFilePath, createLtParameter, ltParameterName, parameterGroup);
                        }
                        return false;
                    }

                    logger.Log($"AddLookupTable: попытка создания таблицы поиска \"{ltName}\"");
                    t.Start();
                    FamilySizeTableErrorInfo ei = new();
                    if (!fstm.ImportSizeTable(doc, ltFilePath, ei))
                    {
                        logger.Log($"AddLookupTable: ошибка импорта таблицы {ltName}: {ei.FamilySizeTableErrorType}.");
                        return false;
                    }
                    logger.Log("AddLookupTable: создание успешно");
                    if(createLtParameter)
                    {
                        ltParameterName = ltParameterName.Replace("{LT}", ltName);
                        bool okToCreateNewParameter = true;
                        foreach (FamilyParameter p in doc.FamilyManager.Parameters)
                            if (p.Definition.Name == ltParameterName)
                            {
                                okToCreateNewParameter = false;
                                logger.Log($"AddLookupTable: параметр {ltParameterName} уже существует.");
                            }
                        if (okToCreateNewParameter)
                        {
                            LT_parameter = doc.FamilyManager.AddParameter(ltParameterName, parameterGroup, ParameterType.Text, false);
                            if (LT_parameter != null)
                            {
                                logger.Log($"AddLookupTable: параметр \"{ltParameterName}\" создан. Попытка задать ему значение \"{ltName}\":");
                                doc.FamilyManager.SetFormula(LT_parameter, "\"" + ltName + "\"");
                                logger.Log($"Ok.");
                            }
                            else
                                logger.Log($"AddLookupTable: ошибка создания параметра \"{ltParameterName}\"");
                        }
                    }
                    t.Commit();
                }
                catch (Exception ex)
                {
                    if (t != null && t.GetStatus() == TransactionStatus.Started)
                        t.RollBack();
                    logger.Log($"AddLookupTable exception: {ex}");
                }
            }
            return true;
        }
    }
}
