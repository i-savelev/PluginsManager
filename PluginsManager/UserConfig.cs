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
            Logger.Info("Инициализация конфигурации пользователя");

            if (!Directory.Exists(PathManager.userFolderFile))
            {
                Directory.CreateDirectory(PathManager.userFolderFile);
                Logger.Info($"Создана папка конфигурации: [{PathManager.userFolderFile}]");
            }
            Logger.Info($"Файл конфигурации пользователя: [{PathManager.userConfigFile}]");
            Logger.Info($"Папка с копиями файлов: [{PathManager.tempDllDir}]");
        }

        public static void GetUserSettings()
        {
            Logger.Info($"Чтение настроек пользователя из [{PathManager.userConfigFile}]");
            if (!File.Exists(PathManager.userConfigFile))
            {
                Logger.Info("Настроек пользователя нет, вызов диалога выбора папки");
                SetPathToUserConfigFileDialog();
            }
            else
            {
                XDocument xmlDoc = XDocument.Load(PathManager.userConfigFile);
                var sourcePath = xmlDoc.Element(Const.UserConfigFile.XmlSettings)?.Element(Const.UserConfigFile.XmlFolderPath)?.Value ?? string.Empty;
                PathManager.SetSourcePath(sourcePath);
                exceptionTabs = xmlDoc.Element(Const.UserConfigFile.XmlSettings)?.Element(Const.UserConfigFile.XmlExceptionTabs)?.Value ?? string.Empty;
                post = xmlDoc.Element(Const.UserConfigFile.XmlSettings)?.Element(Const.UserConfigFile.XmlPost)?.Value ?? string.Empty;
                Logger.Info($"Исходная папка = [{sourcePath}]");
                Logger.Info($"Исключенные вкладки = [{exceptionTabs}]");
                Logger.Info($"Роль = [{post}]");
            }
        }

        public static bool SetPathToUserConfigFileDialog()
        {
            Logger.Info("Открытие диалога выбора папки с DLL");
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (File.Exists(PathManager.userConfigFile))
                {
                    XDocument xmlDoc = XDocument.Load(PathManager.userConfigFile);
                    exceptionTabs = xmlDoc.Element(Const.UserConfigFile.XmlSettings)?.Element(Const.UserConfigFile.XmlExceptionTabs)?.Value ?? string.Empty;
                    post = xmlDoc.Element(Const.UserConfigFile.XmlSettings)?.Element(Const.UserConfigFile.XmlPost)?.Value ?? string.Empty;
                }

                openFileDialog.Title = "Выберете папку с файлами .dll";
                openFileDialog.CheckFileExists = false;
                openFileDialog.CheckPathExists = true;
                openFileDialog.DereferenceLinks = true;
                openFileDialog.FileName = "Выбор папки";
                openFileDialog.Filter = "Все папки|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = Path.GetDirectoryName(openFileDialog.FileName);
                    Logger.Info($"Выбранный путь: [{folderPath}]");
                    PathManager.SetSourcePath(folderPath);
                    XElement settings = new XElement(Const.UserConfigFile.XmlSettings,
                        new XElement(Const.UserConfigFile.XmlFolderPath, PathManager.sourceDir),
                        new XElement(Const.UserConfigFile.XmlExceptionTabs, exceptionTabs),
                        new XElement(Const.UserConfigFile.XmlPost, post)
                    );
                    settings.Save(PathManager.userConfigFile);
                    Logger.Info($"Конфигурация пользователя сохранена: [{PathManager.userConfigFile}]");
                    CommandConfig.CreateConfigFile();
                    TempFiles.CopyToTemp(PathManager.sourceDir);
                    Dllmanager.CopyDll();
                    return true;
                }
                else
                {
                    Logger.Warning("Пользователь отменил выбор папки");
                    return false;
                }
            }
        }
    }
}
