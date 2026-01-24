using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using static PluginsManager.Const;

namespace PluginsManager
{
    public static class UserConfig
    {
        public static string exceptionTabs = string.Empty;
        public static string post = UserConfigFile.DefaultPost;



        public static void Init()
        {
            Logger.Info("UserConfig", $"Инициализация конфигурации пользователя...");

            if (!Directory.Exists(PathManager.userFolderFile))
            {
                Directory.CreateDirectory(PathManager.userFolderFile);
                Logger.Info("UserConfig", $"Папка [{PathManager.userFolderFile}] не существует, создание папки");
            }
            Logger.Info("UserConfig", $"Файл конфигурации пользователя [{PathManager.userConfigFile}]");
            Logger.Info("UserConfig", $"Папка с копиями файлов [{PathManager.tempDllDir}]");
        }

        public static void GetUserSettings()
        {
            Logger.Info("GetUserSettings", $"Получение настроек пользователя в {PathManager.userConfigFile}...");
            if (!File.Exists(PathManager.userConfigFile))
            {
                Logger.Info("GetUserSettings", $"Настроек пользователя нет, вызов диалога выбора папки...");
                SetPathToUserConfigFileDialog();
            }
            else
            {
                Logger.Info("GetUserSettings", $"Настройки ползователя есть. Сбор информации...");
                XDocument xmlDoc = XDocument.Load(PathManager.userConfigFile);
                var sourcePath = xmlDoc.Element(Const.UserConfigFile.XmlSettings)?.Element(Const.UserConfigFile.XmlFolderPath)?.Value ?? string.Empty;
                PathManager.SetSourcePath(sourcePath);
                exceptionTabs = xmlDoc.Element(Const.UserConfigFile.XmlSettings)?.Element(Const.UserConfigFile.XmlExceptionTabs)?.Value ?? string.Empty;
                post = xmlDoc.Element(Const.UserConfigFile.XmlSettings)?.Element(Const.UserConfigFile.XmlPost)?.Value ?? string.Empty;
                Logger.Info("GetUserSettings", $":исходная папка = [{sourcePath}]");
                Logger.Info("GetUserSettings", $":исключенные вкладки = [{exceptionTabs}]");
                Logger.Info("GetUserSettings", $":роль = [{post}]");
            }
        }
        public static bool SetPathToUserConfigFileDialog()
        {
            Logger.Info("SetPathToUserConfigFileDialog", $"Диалог выбора папки...");
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                Logger.Info("SetPathToUserConfigFileDialog", $"Проверка существования настроек пользователя...");
                if (File.Exists(PathManager.userConfigFile))
                {
                    Logger.Info("SetPathToUserConfigFileDialog", $"Уже существует файл настроек пользователя: {PathManager.userConfigFile}");
                    XDocument xmlDoc = XDocument.Load(PathManager.userConfigFile);
                    exceptionTabs = xmlDoc.Element(Const.UserConfigFile.XmlSettings)?.Element(Const.UserConfigFile.XmlExceptionTabs)?.Value ?? string.Empty;
                    post = xmlDoc.Element(Const.UserConfigFile.XmlSettings)?.Element(Const.UserConfigFile.XmlPost)?.Value ?? string.Empty;
                    Logger.Info("SetPathToUserConfigFileDialog", $":исключенные вкладки = [{exceptionTabs}]");
                    Logger.Info("SetPathToUserConfigFileDialog", $":роль = [{post}]");
                }

                openFileDialog.Title = "Выберете папку с файлами .dll";
                openFileDialog.CheckFileExists = false;
                openFileDialog.CheckPathExists = true;
                openFileDialog.DereferenceLinks = true;
                openFileDialog.FileName = "Выбор папки";
                openFileDialog.Filter = "Все папки|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Logger.Info("SetPathToUserConfigFileDialog", $"Папка выбрана...");
                    string folderPath = Path.GetDirectoryName(openFileDialog.FileName);
                    Logger.Info("SetPathToUserConfigFileDialog", $"Выбранный путь {folderPath}");
                    PathManager.SetSourcePath(folderPath);
                    Logger.Info("SetPathToUserConfigFileDialog", $"Обновление настроек ползователя...");
                    XElement settings = new XElement(Const.UserConfigFile.XmlSettings,
                        new XElement(Const.UserConfigFile.XmlFolderPath, PathManager.sourceDir),
                        new XElement(Const.UserConfigFile.XmlExceptionTabs, exceptionTabs),
                        new XElement(Const.UserConfigFile.XmlPost, post)
                    );
                    Logger.Info("SetPathToUserConfigFileDialog", $":исходная папка = [{PathManager.sourceDir}]");
                    Logger.Info("SetPathToUserConfigFileDialog", $":исключенные вкладки = [{exceptionTabs}]");
                    Logger.Info("SetPathToUserConfigFileDialog", $":роль = [{post}]");
                    settings.Save(PathManager.userConfigFile);
                    Logger.Info("SetPathToUserConfigFileDialog", $":Сохранение конигурации пользователя [{PathManager.userConfigFile}]");
                    CommandConfig.CreateConfigFile();
                    TempFiles.CopyToTemp(PathManager.sourceDir);
                    Dllmanager.CopyDll();
                    return true;

                }
                else
                {
                    return false;
                }
            }
        }
    }
}
