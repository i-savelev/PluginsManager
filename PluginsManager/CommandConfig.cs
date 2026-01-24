using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using static PluginsManager.Const;

namespace PluginsManager
{
    public static class CommandConfig
    {
        public static Dictionary<string, Dictionary<string, string>> CommamdConfigDictionary { get; set; }


        private static void GetConfigFilePath()
        {
            Logger.Info("GetConfigFilePath", $"Получение пути к конфигурации команд...");
            if (!string.IsNullOrEmpty(PathManager.sourceDir))
            {
                string pluginConfigPathImg = Path.Combine(PathManager.sourceDir, CmdConfigFile.ImageFolderName);
                Logger.Info("GetConfigFilePath", $"Путь к папке изображений {pluginConfigPathImg}");
                Logger.Info("GetConfigFilePath", $"Путь к папке конфигурации {PathManager.sourceDir}");
                if (!Directory.Exists(PathManager.sourceDir))
                {
                    Directory.CreateDirectory(PathManager.sourceDir);
                }
                if (!Directory.Exists(pluginConfigPathImg))
                {
                    Directory.CreateDirectory(pluginConfigPathImg);
                }
                Logger.Info("GetConfigFilePath", $"Конфигурация: {PathManager.sourceComandConfigFile}");
            }
        }

        public static void GetDllSettings()
        {
            Logger.Info("GetDllSettings", $"Получение конфигурации команд...");
            if (CommamdConfigDictionary != null)
            {
                CommamdConfigDictionary.Clear();
            }

            Dictionary<string, Dictionary<string, string>> commandConfigDictionary = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                XDocument xDoc = XDocument.Load(PathManager.tempComandConfigFile);
                var commands = xDoc.Descendants(CmdConfigFile.XmlCommand);
                foreach (var command in commands)
                {
                    string cmdCode = command.Element(CmdConfigFile.XmlCode[0])?.Value;
                    string cmdTab = command.Element(CmdConfigFile.XmlTab[0])?.Value;
                    string cmdName = command.Element(CmdConfigFile.XmlName[0])?.Value;
                    string cmdDescription = command.Element(CmdConfigFile.XmlDescription[0])?.Value;
                    string cmdImage = command.Element(CmdConfigFile.XmlImage[0])?.Value;
                    if (!commandConfigDictionary.ContainsKey(cmdCode))
                    {
                        commandConfigDictionary[cmdCode] = new Dictionary<string, string>()
                            {
                                {CmdConfigFile.XmlTab[0], cmdTab },
                                {CmdConfigFile.XmlName[0], cmdName },
                                {CmdConfigFile.XmlDescription[0], cmdDescription },
                                {CmdConfigFile.XmlImage[0], cmdImage },
                            };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetDllSettings", $"Ошибка при формирвоании словаря {ex.Message}");
            }
            CommamdConfigDictionary = commandConfigDictionary;
            Logger.Info("GetDllSettings", $"Словарь команд из конфигурации получен");
        }

        public static void CreateConfigFile()
        {
            GetConfigFilePath();
            Logger.Info("CreateConfigFile", $"Создание конфигурации команд...");
            if (!File.Exists(PathManager.sourceComandConfigFile))
            {
                Logger.Info("CreateConfigFile", $"Файла конфигурации нет, создание пустого файла...");
                XElement root = new XElement(CmdConfigFile.XmlRoot);
                XElement cmdCommand = new XElement(CmdConfigFile.XmlCommand,
                    new XElement(CmdConfigFile.XmlCode[0], CmdConfigFile.XmlCode[1]),
                    new XElement(CmdConfigFile.XmlTab[0], CmdConfigFile.XmlTab[1]),
                    new XElement(CmdConfigFile.XmlName[0], CmdConfigFile.XmlName[1]),
                    new XElement(CmdConfigFile.XmlDescription[0], CmdConfigFile.XmlDescription[1]),
                    new XElement(CmdConfigFile.XmlImage[0], CmdConfigFile.XmlImage[1])
                    );
                root.Add(cmdCommand);
                XDocument xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
                xmlDoc.Save(PathManager.sourceComandConfigFile);
            }
            Logger.Info("CreateConfigFile", $"Файл конфигурации есть...");
        }
    }
}
