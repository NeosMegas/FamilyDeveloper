#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FamilyDeveloper.Commands;
using FamilyDeveloper.Helpers;
using FamilyDeveloper.ViewModels;
using SimplePluginLogger;
#endregion

namespace FamilyDeveloper
{
    class App : IExternalApplication
    {
        static AddInId addinId = new AddInId(new Guid("92a2555b-ded7-40f4-9baf-8d211248343b"));

        public static Logger logger;

        string tabName = "Family Developer";

        private ButtonInfo[] buttonInfos = [
            new ButtonInfo() {
                PanelName = "Параметры",
                Name = "AddParametersWithFormulas",
                Text = "Редактирование\nпараметров\nсемейства",
                LinkToCommand = nameof(FamilyDeveloper) + "." + nameof(FamilyDeveloper.Commands) + "." + nameof(ParametersCommand),
                Tooltip = $"Создание, редактирование, удаление параметров семейства\nv{Assembly.GetExecutingAssembly().GetName().Version}",
                LongDescription = "Тут будет подробное описание",
                Image = "Parameters16.png",
                LargeImage = "Parameters32.png",
                HelpUrl = @"https://github.com/NeosMegas/FamilyDeveloper/wiki/Parameters"
            },
            new ButtonInfo() {
                PanelName = "Таблицы поиска",
                Name = "AddLookupTable",
                Text = "Добавление\nтаблицы\nпоиска",
                LinkToCommand = nameof(FamilyDeveloper) + "." + nameof(FamilyDeveloper.Commands) + "." + nameof(AddLookupTableCommand),
                Tooltip = $"Добавление таблицы поиска\nv{Assembly.GetExecutingAssembly().GetName().Version}",
                LongDescription = "Тут будет подробное описание",
                Image = "AddLookupTable16.png",
                LargeImage = "AddLookupTable32.png",
                HelpUrl = @"https://github.com/NeosMegas/FamilyDeveloper/wiki/AddReplaceLookupTable"
            },
            new ButtonInfo() {
                PanelName = "Таблицы поиска",
                Name = "ReplaceLookupTable",
                Text = "Замена\nтаблицы\nпоиска",
                LinkToCommand = nameof(FamilyDeveloper) + "." + nameof(FamilyDeveloper.Commands) + "." + nameof(ReplaceLookupTableCommand),
                Tooltip = $"Замена существующей таблицы поиска\nv{Assembly.GetExecutingAssembly().GetName().Version}",
                LongDescription = "Тут будет подробное описание",
                Image = "ReplaceLookupTable16.png",
                LargeImage = "ReplaceLookupTable32.png",
                HelpUrl = @"https://github.com/NeosMegas/FamilyDeveloper/wiki/AddReplaceLookupTable"
            }
        ];

        public App()
        {
            logger = new Logger();
        }

        /// <summary>
        /// Creating a panel in the Revit tab
        /// </summary>
        /// <param name="application"></param>
        /// <param name="tabName">The name of the tab where the panel should be created. If empty, the panel will be created in the "Add-ins" tab.</param>
        /// <returns>Created RibbonPanel</returns>
        public Dictionary<string, RibbonPanel> CreatePanels(UIControlledApplication application, string tabName = "")
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            HashSet<string> ribbonPanelNames = new HashSet<string>();
            foreach (ButtonInfo buttonInfo in buttonInfos)
                if (!ribbonPanelNames.Contains(buttonInfo.PanelName))
                    ribbonPanelNames.Add(buttonInfo.PanelName);
            Dictionary<string, RibbonPanel> ribbonPanels = new Dictionary<string, RibbonPanel>();
            if (!string.IsNullOrEmpty(tabName))
                try { application.CreateRibbonTab(tabName); } catch { }
            foreach (string panelName in ribbonPanelNames)
            {
                if (string.IsNullOrEmpty(tabName))
                    ribbonPanels.Add(panelName, application.GetRibbonPanels().FirstOrDefault(x => x.Name == panelName) ?? application.CreateRibbonPanel(panelName));
                else
                    ribbonPanels.Add(panelName, application.GetRibbonPanels(tabName).FirstOrDefault(x => x.Name == panelName) ?? application.CreateRibbonPanel(tabName, panelName));
            }
            foreach (ButtonInfo info in buttonInfos)
                AddButton(ribbonPanels[info.PanelName], info.Name, info.Text, assemblyPath, info.LinkToCommand, info.Tooltip, info.LongDescription, info.Image, info.LargeImage, info.HelpUrl);
            return ribbonPanels;
        }

        /// <summary>
        /// Adds a button to the created panel
        /// </summary>
        /// <param name="ribbonPanel">The panel to add the button to</param>
        /// <param name="buttonName">Button name (and text)</param>
        /// <param name="path">Assembly path</param>
        /// <param name="linkToCommand">Link to command</param>
        /// <param name="toolTip">Button tooltip</param>
        /// <param name="longDescription">Command description</param>
        private void AddButton(RibbonPanel ribbonPanel, string buttonName, string buttonText, string path, string linkToCommand, string toolTip, string longDescription, string image = "", string largeImage = "", string helpUrl = "")
        {
            PushButtonData buttonData = new PushButtonData(
               buttonName,
               buttonText,
               path,
               linkToCommand);
            if (helpUrl != "")
                buttonData.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, helpUrl));
            PushButton button = ribbonPanel.AddItem(buttonData) as PushButton;
            if (button != null)
            {
                button.ToolTip = toolTip;
                Uri largeImageUri = null, imageUri = null;
                if (largeImage != "")
                {
                    largeImageUri = new Uri($@"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/{largeImage}", UriKind.RelativeOrAbsolute);
                    if (largeImageUri != null)
                        button.LargeImage = new BitmapImage(largeImageUri);
                }
                if (image != "")
                {
                    imageUri = new Uri($@"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/{image}", UriKind.RelativeOrAbsolute);
                    if (imageUri != null)
                        button.Image = new BitmapImage(imageUri);
                }
                button.LongDescription = longDescription;
                //button.ToolTipImage = new BitmapImage(new Uri($@"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/ToolTipImage.png", UriKind.RelativeOrAbsolute));
            }
        }

        //string commandNameCommand = @"CustomCtrl_%CustomCtrl_%Add-Ins%FamilyDeveloper%Test";

        public Result OnStartup(UIControlledApplication a)
        {
            CreatePanels(a, tabName);
            //a.ControlledApplication.DocumentOpened += ControlledApplication_DocumentOpened;
            //RevitCommandId commandId = RevitCommandId.LookupCommandId(commandNameCommand);
            //a.GetUIApplication().PostCommand(commandId);
            return Result.Succeeded;
        }

        //private void ControlledApplication_DocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
        //{
        //    TaskDialog.Show("1", "Document opened");
        //}

        public Result OnShutdown(UIControlledApplication a)
        {
            //a.ControlledApplication.DocumentOpened -= ControlledApplication_DocumentOpened;
            return Result.Succeeded;
        }

        private class ButtonInfo
        {
            public string PanelName { get; set; }
            public string Name { get; set; }
            public string Text { get; set; }
            public string LinkToCommand { get; set; }
            public string Tooltip { get; set; }
            public string LongDescription { get; set; }
            public string Image { get; set; }
            public string LargeImage { get; set; }
            public string HelpUrl { get; set; }
        }

    }
}
