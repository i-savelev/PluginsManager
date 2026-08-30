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
            Logger.Info("Получение пути к конфигурации команд");
            if (!string.IsNullOrEmpty(PathManager.sourceDir))
            {
                string pluginConfigPathImg = Path.Combine(PathManager.sourceDir, CmdConfigFile.ImageFolderName);
                Logger.Info($"Путь к папке изображений: [{pluginConfigPathImg}]");
                Logger.Info($"Путь к папке конфигурации: [{PathManager.sourceDir}]");
                if (!Directory.Exists(PathManager.sourceDir))
                {
                    Directory.CreateDirectory(PathManager.sourceDir);
                }
                if (!Directory.Exists(pluginConfigPathImg))
                {
                    Directory.CreateDirectory(pluginConfigPathImg);
                }
                Logger.Info($"Файл конфигурации команд: [{PathManager.sourceComandConfigFile}]");
            }
        }

        public static void GetDllSettings()
        {
            Logger.Info($"Чтение конфигурации команд из [{PathManager.tempComandConfigFile}]");
            if (CommamdConfigDictionary != null)
            {
                CommamdConfigDictionary.Clear();
            }

            Dictionary<string, Dictionary<string, string>> commandConfigDictionary = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                XDocument xDoc = XDocument.Load(PathManager.tempComandConfigFile);
                var commands = xDoc.Descendants(CmdConfigFile.XmlCommand);
                int count = 0;
                foreach (var command in commands)
                {
                    string cmdCode = command.Element(CmdConfigFile.XmlCode[0])?.Value ?? string.Empty;
                    string cmdTab = command.Element(CmdConfigFile.XmlTab[0])?.Value ?? string.Empty;
                    string cmdName = command.Element(CmdConfigFile.XmlName[0])?.Value ?? string.Empty;
                    string cmdDescription = command.Element(CmdConfigFile.XmlDescription[0])?.Value ?? string.Empty;
                    string cmdImage = command.Element(CmdConfigFile.XmlImage[0])?.Value ?? string.Empty;
                    if (!commandConfigDictionary.ContainsKey(cmdCode))
                    {
                        commandConfigDictionary[cmdCode] = new Dictionary<string, string>()
                        {
                            {CmdConfigFile.XmlTab[0], cmdTab },
                            {CmdConfigFile.XmlName[0], cmdName },
                            {CmdConfigFile.XmlDescription[0], cmdDescription },
                            {CmdConfigFile.XmlImage[0], cmdImage },
                        };
                        count += 1;
                    }
                }
                Logger.Info($"Конфигурация команд загружена: {count} записей");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Ошибка при формировании словаря команд");
            }
            CommamdConfigDictionary = commandConfigDictionary;
        }

        public static void CreateConfigFile()
        {
            GetConfigFilePath();
            Logger.Info("Проверка конфигурации команд");
            if (!File.Exists(PathManager.sourceComandConfigFile))
            {
                Logger.Info("Файл конфигурации не найден, создается шаблон");
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
                Logger.Info($"Шаблон конфигурации создан: [{PathManager.sourceComandConfigFile}]");
            }
            else
            {
                Logger.Info("Файл конфигурации команд уже существует");
            }
        }
    }
}
