using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using static PluginsManager.Const;
using Path = System.IO.Path;

namespace PluginsManager
{
    public class CommandManager
    {
        public UIApplication UiApp { get; set; }
        public List<Type> AllTypes = new List<Type>();
        public List<Command> AllCommands = new List<Command>();
        public Dictionary<string, List<Command>> CommandsDictionary = new Dictionary<string, List<Command>>();
        public string FolderPath { get; set; }
        public ExternalEvent ExternalEvent { get; set; }


        public CommandManager(UIApplication uiApp, string folderPath)
        {
            UiApp = uiApp;
            FolderPath = folderPath;
            GetExternalCommandsFromAssembly();
            Handler eventHandler = new Handler(this);
            ExternalEvent externalEvent = ExternalEvent.Create(eventHandler);
            ExternalEvent = externalEvent;

        }

        public void Refresh(string folderPath)
        {
            Logger.Info("Refresh", "Обновление");
            AllCommands.Clear();
            AllTypes.Clear();
            CommandsDictionary.Clear();
            FolderPath = folderPath;
            GetExternalCommandsFromAssembly();
            Handler eventHandler = new Handler(this);
            ExternalEvent externalEvent = ExternalEvent.Create(eventHandler);
            ExternalEvent = externalEvent;
        }

        public void RunCommand(string commandName)
        {
            Logger.Info("RunCommand", $"Запуск команды [{commandName}]");
            var commandType = AllTypes.FirstOrDefault(x => x.FullName == commandName);
            IExternalCommand commandInstance = (IExternalCommand)Activator.CreateInstance(commandType);
            ExternalCommandData commandData = Create(UiApp);
            string message = string.Empty;
            ElementSet elements = null;
            Result result = commandInstance.Execute(commandData, ref message, elements);
            if (result != Result.Succeeded)
            {
                TaskDialog.Show("Ошибка", message);
                Logger.Error("RunCommand", $"Ошибка запуска команды [{commandName}, {message}]");
            }
        }

        public ExternalCommandData Create(UIApplication uiApplication)
        {
            // Находим тип ExternalCommandData
            Type externalCommandDataType = typeof(ExternalCommandData);

            // Находим внутренний конструктор
            ConstructorInfo constructor = externalCommandDataType
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault();

            if (constructor is null)
            {
                throw new InvalidOperationException("Не удалось найти конструктор ExternalCommandData.");
            }

            // Создаем экземпляр через рефлексию
            ExternalCommandData data = (ExternalCommandData)constructor.Invoke(null);

            // Устанавливаем свойство Application через рефлексию
            PropertyInfo applicationProperty = externalCommandDataType.GetProperty(
                Const.PropertyNames.Application,
                BindingFlags.Public | BindingFlags.Instance
            );

            if (applicationProperty != null && applicationProperty.CanWrite)
            {
                applicationProperty.SetValue(data, uiApplication);
            }
            else
            {
                throw new InvalidOperationException("Не удалось установить свойство Application.");
            }

            return data;
        }

        private void GetExternalCommandsFromAssembly()
        {
            var i = 0;
            Logger.Separator();
            Logger.Info("GetExternalCommandsFromAssembly", $"Получение сборок из dll...");
            try
            {
                foreach (var dllFile in Dllmanager.DllList())
                {
                    i += 1;
                    try
                    {
                        Logger.Info("GetExternalCommandsFromAssembly", $"[{i}]: {dllFile}");
                        Logger.Info("GetExternalCommandsFromAssembly", $"Поптыка загрузить сборку из текущего AppDomain...");
                        var assembly = FindLoadedAssemblyByLocation(dllFile);
                        if (assembly == null)
                        {
                            Logger.Info("GetExternalCommandsFromAssembly", $"В текущем AppDomain сборка не найдена, загрузка из файла...");
                            
                            if (File.Exists(dllFile + ":Zone.Identifier"))
                            {
                                File.Delete(dllFile + ":Zone.Identifier");
                            }
                            assembly = Assembly.LoadFile(dllFile);
                        }
                        if (assembly == null || !IsAPIReferenced(assembly))
                        {
                            Logger.Info("GetExternalCommandsFromAssembly", $"не IsAPIReferenced");
                            continue;
                        }

                        IEnumerable<Type> externalCommands = assembly.GetTypes()
                            .Where(type => typeof(IExternalCommand).IsAssignableFrom(type) && !type.IsAbstract);

                        AllTypes.AddRange(externalCommands);

                        foreach (var type in externalCommands)
                        {
                            FillCommandsDictionaryAndList(type, assembly, dllFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("GetExternalCommandsFromAssembly", $"dll = [{dllFile}] - {ex.Message}");
                    }
                }
                Logger.Separator();
                SortCommandsDictionary();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка загрузки", ex.Message);
                Logger.Warning("GetExternalCommandsFromAssembly", $"{ex.Message}");
            }
        }

        private Assembly FindLoadedAssemblyByLocation(string dllPath)
        {
            string fullPath = Path.GetFullPath(dllPath); // нормализуем путь

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    // Location может быть пустым (например, для динамических сборок)
                    if (!string.IsNullOrEmpty(assembly.Location))
                    {
                        string loadedPath = Path.GetFullPath(assembly.Location);
                        if (string.Equals(fullPath, loadedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Info("FindLoadedAssemblyByLocation", $"Сборка найдена в текущем AppDomain");
                            Logger.Info("FindLoadedAssemblyByLocation", $"{assembly.FullName}");
                            return assembly;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Некоторые сборки могут выбросить исключение при доступе к Location
                    Logger.Warning("FindLoadedAssemblyByLocation", $"Ошибка при чтении Location для сборки: {ex.Message}");
                }
            }

            return null;
        }

        private void FillCommandsDictionaryAndList(Type type, Assembly assembly, string dllFilePath)
        {
            Logger.Info("FillCommandsDictionaryAndList", $"Добавление команды {type.FullName} в словарь...");
            var commandName = string.Empty;
            var tabName = string.Empty;
            var commandDescription = string.Empty;
            var commandImage = string.Empty;

            if (CommandConfig.CommamdConfigDictionary.ContainsKey(type.FullName))
            {
                Logger.Info("FillCommandsDictionaryAndList", $"[{type.FullName}] есть в конфигарции команд");
                commandName = CommandConfig.CommamdConfigDictionary[type.FullName][CmdConfigFile.XmlName[0]];
                tabName = CommandConfig.CommamdConfigDictionary[type.FullName][CmdConfigFile.XmlTab[0]];
                commandDescription = CommandConfig.CommamdConfigDictionary[type.FullName][CmdConfigFile.XmlDescription[0]];
                commandImage = CommandConfig.CommamdConfigDictionary[type.FullName][CmdConfigFile.XmlImage[0]];
                Logger.Info("FillCommandsDictionaryAndList", $"Название = {commandName} Вкладка={tabName}");
            }
            else
            {
                Logger.Info("FillCommandsDictionaryAndList", $"[{type.FullName}] нет в конфигарции команд");
                commandName = type.GetProperty(Const.DllFields.Name, BindingFlags.Public | BindingFlags.Static)
                    ?.GetValue(null)
                    ?.ToString();
                tabName = type.GetProperty(Const.DllFields.TabName, BindingFlags.Public | BindingFlags.Static)
                    ?.GetValue(null)
                    ?.ToString();
                commandDescription = type.GetProperty(Const.DllFields.Description, BindingFlags.Public | BindingFlags.Static)
                    ?.GetValue(null)
                    ?.ToString();
                commandImage = type.GetProperty(Const.DllFields.Image, BindingFlags.Public | BindingFlags.Static)
                    ?.GetValue(null)
                    ?.ToString();
                Logger.Info("FillCommandsDictionaryAndList", $"Название = {commandName} Вкладка={tabName}");
            }
            if (!string.IsNullOrEmpty(tabName))
            {
                Image image = Properties.Resources.imgPlaceholder;

                if (CommandConfig.CommamdConfigDictionary.ContainsKey(type.FullName))
                {
                    var path = System.IO.Path.Combine(FolderPath, CmdConfigFile.ImageFolderName, commandImage);

                    if (File.Exists(path))
                    {
                        byte[] imageBytes = File.ReadAllBytes(path);
                        try
                        {
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            {
                                image = Image.FromStream(ms);
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(commandImage))
                    {
                        using (Stream stream = assembly.GetManifestResourceStream(commandImage))
                        {
                            if (stream != null)
                            {
                                image = Image.FromStream(stream);
                            }
                        }
                    }
                }
                var command = new Command(type.FullName, commandName, commandDescription, image, dllFilePath);
                AllCommands.Add(command);

                if (tabName.Contains("|"))
                {
                    foreach (var tab in tabName.Split('|'))
                    {
                        if (!CommandsDictionary.ContainsKey(tab))
                        {
                            CommandsDictionary.Add(tab, new List<Command> { command });
                        }
                        else
                        {
                            CommandsDictionary[tab].Add(command);
                        }
                    }
                }
                else
                {
                    if (!CommandsDictionary.ContainsKey(tabName))
                    {
                        CommandsDictionary.Add(tabName, new List<Command> { command });
                    }
                    else
                    {
                        CommandsDictionary[tabName].Add(command);
                    }
                }
            }
        }
        private void SortCommandsDictionary()
        {
            Logger.Info("SortCommandsDictionary", $"Сортировка команд");
            if (CommandsDictionary != null)
            {
                CommandsDictionary = CommandsDictionary
                    .OrderBy(pair => pair.Key)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                foreach (var key in CommandsDictionary.Keys.ToList())
                {
                    CommandsDictionary[key] = CommandsDictionary[key].OrderBy(val => val.CmdName).ToList();
                }
            }
        }

        private bool IsAPIReferenced(Assembly assembly)
        {
            if (assembly == null) return false;
            foreach (var refName in assembly.GetReferencedAssemblies())
            {
                if (string.Equals(refName.Name, "RevitAPI", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("IsAPIReferenced", $"{assembly.FullName} содержит RevitAPI");
                    return true;
                }

            }
            return false;
        }
    }
}
