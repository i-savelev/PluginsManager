using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using System.IO;

namespace PluginsManager
{
    [Transaction(TransactionMode.Manual)]
    public class PluginManager : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Logger.Clear();
            Logger.Info("main", "Приложение запущено");
            UIApplication uiApp = commandData.Application;
            PathManager.Init();
            UserConfig.Init();

            TempFiles.Init();

            UserConfig.GetUserSettings();

            CommandConfig.GetDllSettings();

            Dllmanager.LoadDllListFromXml();

            CommandManager commandManager = new CommandManager(uiApp, PathManager.tempDllDir);

            WindowManager windowManager = new WindowManager(commandManager, uiApp);
            IsDebugWindow.Show();
            return Result.Succeeded;
        }
    }
}
