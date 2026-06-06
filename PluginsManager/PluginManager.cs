using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PluginsManager
{
    [Transaction(TransactionMode.Manual)]
    public class PluginManager : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            var revitApp = uiApp?.Application;

            Logger.Init(
                hostName: revitApp?.VersionName,
                hostVersionNumber: revitApp?.VersionNumber,
                hostBuild: revitApp?.VersionBuild,
                hasActiveDocument: uiApp.ActiveUIDocument != null);
            Logger.Info("Команда PluginsManager запущена");

            PathManager.Init();
            UserConfig.Init();
            TempFiles.Init();

            UserConfig.GetUserSettings();
            CommandConfig.GetDllSettings();
            Dllmanager.LoadDllListFromXml();

            CommandManager commandManager = new CommandManager(uiApp, PathManager.tempDllDir);
            Logger.Info($"Команды загружены: {commandManager.AllCommands.Count} | вкладки: {commandManager.CommandsDictionary.Count}");

            WindowManager windowManager = new WindowManager(commandManager, uiApp);
            Logger.Info("Основное окно менеджера закрыто");
            IsDebugWindow.Show();

            return Result.Succeeded;
        }
    }
}
