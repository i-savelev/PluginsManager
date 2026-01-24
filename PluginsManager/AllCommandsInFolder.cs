using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;

namespace PluginsManager
{
    [Transaction(TransactionMode.Manual)]
    public class AllCommandsInFolder : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;


            PathManager.Init();
            UserConfig.Init();

            TempFiles.Init();

            UserConfig.GetUserSettings();

            CommandConfig.GetDllSettings();

            Dllmanager.LoadDllListFromXml();

            CommandManager commandManager = new CommandManager(uiApp, PathManager.tempDllDir);

            foreach (var type in commandManager.AllTypes)
            {
                IsDebugWindow.AddRow(type.FullName);
            }
            IsDebugWindow.Show();


            return Result.Succeeded;
        }
    }
}
