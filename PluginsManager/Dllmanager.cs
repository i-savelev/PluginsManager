using PluginsManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

public static class Dllmanager
{
    private static readonly List<string> _sourceDllList = new List<string>();
    private static readonly List<string> _parserDllList = new List<string>();

    private static void _findDll(string rootFolderPath)
    {
        if (string.IsNullOrWhiteSpace(rootFolderPath) || !Directory.Exists(rootFolderPath))
            return;

        _sourceDllList.Clear(); // очищаем предыдущие результаты

        try
        {
            // Ищем все .dll рекурсивно
            var dllFiles = Directory.GetFiles(rootFolderPath, "*.dll", SearchOption.AllDirectories);

            foreach (string dllPath in dllFiles)
            {
                _sourceDllList.Add(Path.GetFullPath(dllPath));
                //IsDebugWindow.AddRow(dllPath);
            }
        }
        catch (Exception ex)
        {
            // Например, нет доступа к какой-то папке
            //IsDebugWindow.AddRow($"Ошибка поиска DLL: {ex.Message}");
        }
    }

    public static void CopyDll()
    {
        _parserDllList.Clear();
        Dllmanager._findDll(PathManager.tempDllDir);
        Logger.Info("CopyDll", $"Копирование dll во временные папки...");
        Logger.Info("CopyDll", $"Исходная папка {PathManager.tempDllDir}");

        _clearFolder(PathManager.parsingDllDir);

        foreach (string sourcePath in _sourceDllList)
        {

            if (!File.Exists(sourcePath))
                continue;
            string fileName = Path.GetFileName(sourcePath);
            var fileInfo = new FileInfo(sourcePath);
            DateTime creationTime = fileInfo.LastWriteTime;


            var name = $"{fileName.Replace(".dll", "")}&{creationTime:yyyyMMddHHmmssfff}";
            string targetDir = Path.Combine(PathManager.parsingDllDir, name);
            Directory.CreateDirectory(targetDir);


            string destPath = Path.Combine(targetDir, fileName);
            try
            {
                File.Copy(sourcePath, destPath, overwrite: false);
            }
            catch (Exception ex)
            {
                Logger.Info("CopyDll", $"dll уже существует {destPath}");
                Logger.Warning("CopyDll", $"{ex.Message}");
            }

            _parserDllList.Add(destPath);
            Logger.Info("CopyDll", $"dll: {destPath}");

        }
        _saveDllListToXml(PathManager.tempDllDir);
    }

    private static void _saveDllListToXml(string folder)
    {
        Logger.Info("SaveDllListToXml", $"Сохранение путей к dll в xml файл...");

        var xmlFilePath = Path.Combine(folder, "dll_path.xml");
        Logger.Info("SaveDllListToXml", $"Путь к файлу: {xmlFilePath}");
        if (_parserDllList == null || _parserDllList.Count == 0)
        {
            Logger.Warning("SaveDllListToXml", $"Списоу dll пустой");
            return;
        }
        try
        {
            XDocument doc;

            // Загружаем существующий XML или создаём новый
            if (File.Exists(xmlFilePath))
            {
                doc = XDocument.Load(xmlFilePath);
                Logger.Info("SaveDllListToXml", $"Файл {xmlFilePath} существует");
            }
            else
            {
                doc = new XDocument(new XElement("root"));
                Logger.Info("SaveDllListToXml", $"Файла {xmlFilePath} не существует. Создание нового");
            }

            // Убедимся, что есть корневой элемент
            if (doc.Root == null)
            {
                doc.Add(new XElement("root"));
            }

            var root = doc.Root;

            // Опционально: очистить старые записи, если нужно хранить только актуальные
            root.Elements("dll").Remove();

            // Добавляем каждый путь как <dll>...</dll>
            foreach (string dllPath in _parserDllList)
            {
                root.Add(new XElement("dll", dllPath));
            }

            // Сохраняем с отступами для читаемости
            doc.Save(xmlFilePath);
        }
        catch (Exception ex)
        {
            Logger.Warning("SaveDllListToXml", $"Ошибка сохранения XML: {ex.Message}");
        }
    }

    public static void LoadDllListFromXml()
    {
        _parserDllList.Clear();
        var xmlFilePath = PathManager.parsingDllListPath;
        Logger.Info("LoadDllListFromXml", $"Чтение списка dll из файла {xmlFilePath}");
        if (string.IsNullOrWhiteSpace(xmlFilePath) || !File.Exists(xmlFilePath))
        {
            Logger.Warning("LoadDllListFromXml", $"XML-файл не найден или путь пуст.");
        }
        try
        {
            XDocument doc = XDocument.Load(xmlFilePath);

            // Убеждаемся, что есть корневой элемент
            if (doc.Root == null)
            {
                Logger.Info("LoadDllListFromXml", $"XML-файл пуст или не содержит корневого элемента.");
            }

            // Извлекаем все элементы <dll>
            var dllElements = doc.Root.Elements("dll");

            foreach (var element in dllElements)
            {
                string path = element.Value?.Trim();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    _parserDllList.Add(path);
                    Logger.Info("LoadDllListFromXml", $"DLL: {path}");
                }
                else if (!string.IsNullOrEmpty(path))
                {
                    Logger.Warning("LoadDllListFromXml", $"DLL не найден: {path}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warning("LoadDllListFromXml", $"Ошибка загрузки XML: {ex.Message}");
        }
    }

    public static List<string> DllList()
    {
        Logger.Debug("DllList", $"Список актуальных dll...");
        foreach (var dll in _parserDllList)
        {
            Logger.Debug("DllList", $"{dll}");
        }
        return _parserDllList;
    }

    private static void _clearFolder(string folder)
    {
        Logger.Info("ClearFolder", $"Удаление неиспользуемых сборок...");
        try
        {
            if (Directory.Exists(folder))
            {
                foreach (string subDir in Directory.GetDirectories(folder))
                {
                    try
                    {
                        Directory.Delete(subDir, recursive: true);
                        Logger.Info("ClearFolder", $"Удалена старая подпапка: {subDir}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("ClearFolder", $"Не удалось удалить подпапку {subDir}: {ex.Message}");
                        // Продолжаем, даже если не удалось удалить одну из папок
                    }
                }
            }
            else
            {
                Logger.Error("ClearFolder", $"Папка не существует {folder}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("ClearFolder", $"Ошибка при очистке директории {folder}: {ex.Message}");
        }
    }
}