using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Logger.Info($"Создание менеджера команд для папки [{FolderPath}]");
            GetExternalCommandsFromAssembly();
            Handler eventHandler = new Handler(this);
            ExternalEvent = ExternalEvent.Create(eventHandler);
            Logger.Info("ExternalEvent создан");
        }

        public void Refresh(string folderPath)
        {
            Logger.Info($"Обновление менеджера команд | previousCommands = {AllCommands.Count}, previousTabs = {CommandsDictionary.Count}, newFolder = [{folderPath}]");
            AllCommands.Clear();
            AllTypes.Clear();
            CommandsDictionary.Clear();
            FolderPath = folderPath;
            GetExternalCommandsFromAssembly();
            Handler eventHandler = new Handler(this);
            ExternalEvent = ExternalEvent.Create(eventHandler);
            Logger.Info("ExternalEvent создан");
        }

        public void RunCommand(string commandName)
        {
            var stopwatch = Stopwatch.StartNew();
            Logger.Info($"Запуск команды [{commandName}]");
            var commandType = AllTypes.FirstOrDefault(x => x.FullName == commandName);
            IExternalCommand commandInstance = (IExternalCommand)Activator.CreateInstance(commandType);
            ExternalCommandData commandData = Create(UiApp);
            string message = string.Empty;
            ElementSet elements = null;
            Result result = commandInstance.Execute(commandData, ref message, elements);
            stopwatch.Stop();
            Logger.Info($"Результат команды [{commandName}] = {result} | message = [{message}] | elapsed = {stopwatch.ElapsedMilliseconds} ms");
            if (result != Result.Succeeded)
            {
                TaskDialog.Show("Ошибка", message);
                Logger.Error($"Ошибка запуска команды [{commandName}, {message}]");
            }
        }

        public ExternalCommandData Create(UIApplication uiApplication)
        {
            Type externalCommandDataType = typeof(ExternalCommandData);
            ConstructorInfo constructor = externalCommandDataType
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault();

            if (constructor is null)
            {
                throw new InvalidOperationException("Не удалось найти конструктор ExternalCommandData.");
            }

            ExternalCommandData data = (ExternalCommandData)constructor.Invoke(null);
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

            Logger.Debug("ExternalCommandData успешно создан через reflection");
            return data;
        }

        private void GetExternalCommandsFromAssembly()
        {
            var stopwatch = Stopwatch.StartNew();
            int processedDlls = 0;
            int commandsAdded = 0;
            Logger.Separator();
            Logger.Info("Получение команд из DLL");
            try
            {
                var dllFiles = Dllmanager.DllList().ToList();
                Logger.Info($"DLL для анализа: {dllFiles.Count}");

                foreach (var dllFile in dllFiles)
                {
                    processedDlls += 1;
                    try
                    {
                        Logger.Info($"[{processedDlls}/{dllFiles.Count}] Анализ DLL [{dllFile}]");
                        var assembly = FindLoadedAssemblyByLocation(dllFile);
                        if (assembly == null)
                        {
                            Logger.Info("Сборка не найдена в AppDomain, загрузка из файла");

                            if (File.Exists(dllFile + ":Zone.Identifier"))
                            {
                                File.Delete(dllFile + ":Zone.Identifier");
                                Logger.Debug($"Удален Zone.Identifier для [{dllFile}]");
                            }
                            assembly = Assembly.LoadFile(dllFile);
                        }
                        if (assembly == null || !IsAPIReferenced(assembly))
                        {
                            Logger.Info($"Сборка [{assembly?.FullName ?? dllFile}] пропущена: нет ссылки на RevitAPI");
                            continue;
                        }

                        var externalCommands = assembly.GetTypes()
                            .Where(type => typeof(IExternalCommand).IsAssignableFrom(type) && !type.IsAbstract)
                            .ToList();

                        Logger.Info($"В сборке [{assembly.GetName().Name}] найдено external-команд: {externalCommands.Count}");
                        AllTypes.AddRange(externalCommands);

                        foreach (var type in externalCommands)
                        {
                            if (FillCommandsDictionaryAndList(type, assembly, dllFile))
                            {
                                commandsAdded += 1;
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        var loaderErrors = ex.LoaderExceptions == null
                            ? string.Empty
                            : string.Join(" || ", ex.LoaderExceptions.Where(x => x != null).Select(x => x.Message));
                        Logger.Exception(ex, $"Ошибка загрузки типов из [{dllFile}] | LoaderExceptions = {loaderErrors}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex, $"Ошибка анализа DLL [{dllFile}]");
                    }
                }
                Logger.Separator();
                SortCommandsDictionary();
                stopwatch.Stop();
                Logger.Info($"Анализ DLL завершен за {stopwatch.ElapsedMilliseconds} ms | processedDlls = {processedDlls}, discoveredTypes = {AllTypes.Count}, addedCommands = {commandsAdded}, tabs = {CommandsDictionary.Count}");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка загрузки", ex.Message);
                Logger.Exception(ex, "Критическая ошибка загрузки DLL");
            }
        }

        private Assembly FindLoadedAssemblyByLocation(string dllPath)
        {
            string fullPath = Path.GetFullPath(dllPath);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (!string.IsNullOrEmpty(assembly.Location))
                    {
                        string loadedPath = Path.GetFullPath(assembly.Location);
                        if (string.Equals(fullPath, loadedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Debug($"Сборка найдена в текущем AppDomain: [{assembly.FullName}]");
                            return assembly;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "Ошибка при чтении Location для загруженной сборки", Logger.LogLevel.Warning);
                }
            }

            return null;
        }

        private bool FillCommandsDictionaryAndList(Type type, Assembly assembly, string dllFilePath)
        {
            Logger.Info($"Регистрация команды [{type.FullName}]");
            var commandName = string.Empty;
            var tabName = string.Empty;
            var commandDescription = string.Empty;
            var commandImage = string.Empty;
            var metadataSource = "dll metadata";

            if (CommandConfig.CommamdConfigDictionary.ContainsKey(type.FullName))
            {
                metadataSource = "commands_config.xml";
                commandName = CommandConfig.CommamdConfigDictionary[type.FullName][CmdConfigFile.XmlName[0]];
                tabName = CommandConfig.CommamdConfigDictionary[type.FullName][CmdConfigFile.XmlTab[0]];
                commandDescription = CommandConfig.CommamdConfigDictionary[type.FullName][CmdConfigFile.XmlDescription[0]];
                commandImage = CommandConfig.CommamdConfigDictionary[type.FullName][CmdConfigFile.XmlImage[0]];
            }
            else
            {
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
            }
            Logger.Info($"Metadata source = {metadataSource} | name = [{commandName}] | tab = [{tabName}] | image = [{commandImage}]");

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
                            Logger.Debug($"Изображение загружено из файла [{path}]");
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception(ex, $"Не удалось загрузить изображение [{path}]", Logger.LogLevel.Warning);
                        }
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
                                Logger.Debug($"Изображение загружено из ресурсов сборки [{commandImage}]");
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
                            Logger.Info($"Создана новая вкладка [{tab}]");
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
                        Logger.Info($"Создана новая вкладка [{tabName}]");
                    }
                    else
                    {
                        CommandsDictionary[tabName].Add(command);
                    }
                }

                Logger.Info($"Команда [{type.FullName}] добавлена во вкладки");
                return true;
            }

            Logger.Warning($"Команда [{type.FullName}] пропущена: вкладка не задана");
            return false;
        }

        private void SortCommandsDictionary()
        {
            Logger.Info("Сортировка словаря команд");
            if (CommandsDictionary != null)
            {
                CommandsDictionary = CommandsDictionary
                    .OrderBy(pair => pair.Key)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                foreach (var key in CommandsDictionary.Keys.ToList())
                {
                    CommandsDictionary[key] = CommandsDictionary[key].OrderBy(val => val.CmdName).ToList();
                    Logger.Debug($"Вкладка [{key}] после сортировки содержит {CommandsDictionary[key].Count} команд");
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
                    Logger.Debug($"Сборка [{assembly.FullName}] содержит ссылку на RevitAPI");
                    return true;
                }

            }
            return false;
        }
    }
}
