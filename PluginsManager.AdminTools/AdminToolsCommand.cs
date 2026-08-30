using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;

namespace PluginsManager.AdminTools
{
    [Transaction(TransactionMode.Manual)]
    public class AdminToolsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ConfigureLogging(commandData);

            try
            {
                Logger.Info("[AdminToolsCommand] Старт команды");

                InitializePaths();
                LoadUserSettings();
                CopyFilesToTemp();
                LoadCommandConfig();

                var uiApp = GetUIApplication(commandData); // Передаем commandData
                var commandManager = new CommandManager(uiApp, PathManager.tempDllDir);

                Logger.Info($"[AdminToolsCommand] Загружено команд: {commandManager.AllCommands.Count}");

                if (commandManager.AllCommands.Count == 0)
                {
                    TaskDialog.Show("PluginsManager AdminTools", "Команды не найдены. Проверьте путь к папке с плагинами.");
                    Logger.Warning("[AdminToolsCommand] Список команд пуст");
                    return Result.Succeeded;
                }

                ShowConfigurationForm(commandManager);
                Logger.Info("[AdminToolsCommand] Команда завершена успешно");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "[AdminToolsCommand] Ошибка выполнения команды");
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void ConfigureLogging(ExternalCommandData commandData)
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Temp",
                "PluginsManager",
                "logs",
                "admintools.log");

            Logger.SetLogPath(logPath);
            Logger.SetLogLevel(Logger.LogLevel.Debug);
            Logger.Init(
                hostName: "Autodesk Revit",
                hostVersionNumber: commandData.Application.Application.VersionNumber,
                hostBuild: commandData.Application.Application.VersionBuild,
                hasActiveDocument: commandData.Application.ActiveUIDocument != null);
        }

        private void InitializePaths()
        {
            Logger.Info("[AdminToolsCommand] Инициализация путей");
            PathManager.Init();
        }

        private void LoadUserSettings()
        {
            Logger.Info("[AdminToolsCommand] Загрузка настроек пользователя");
            UserConfig.Init();
            UserConfig.GetUserSettings();

            if (string.IsNullOrEmpty(PathManager.sourceDir))
            {
                Logger.Warning("[AdminToolsCommand] Путь к исходной папке не задан");
                throw new InvalidOperationException("Не задан путь к папке с плагинами. Запустите PluginsManager и выберите папку.");
            }
        }

        private void CopyFilesToTemp()
        {
            Logger.Info("[AdminToolsCommand] Копирование файлов во временную папку");
            TempFiles.Init();
            TempFiles.CopyToTemp(PathManager.sourceDir);
            Dllmanager.CopyDll();
        }

        private void LoadCommandConfig()
        {
            Logger.Info("[AdminToolsCommand] Загрузка конфигурации команд");
            CommandConfig.GetDllSettings();
        }

        private UIApplication GetUIApplication(ExternalCommandData commandData)
        {
            var appType = typeof(Autodesk.Revit.ApplicationServices.Application);
            var uiAppType = typeof(UIApplication);
            var constructor = uiAppType.GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { appType },
                null);

            if (constructor == null)
                throw new InvalidOperationException("Не удалось получить UIApplication");

            return (UIApplication)constructor.Invoke(new object[] { commandData.Application.Application });
        }

        private void ShowConfigurationForm(CommandManager commandManager)
        {
            Logger.Info("[AdminToolsCommand] Открытие формы конфигурации");
            using (var form = new AdminToolsForm(commandManager))
            {
                form.ShowDialog();
            }
            Logger.Info("[AdminToolsCommand] Форма конфигурации закрыта");
        }
    }
}
