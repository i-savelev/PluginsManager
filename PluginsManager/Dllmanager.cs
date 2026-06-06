using PluginsManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public static class Dllmanager
{
    private static readonly List<string> _sourceDllList = new List<string>();
    private static readonly List<string> _parserDllList = new List<string>();

    private static void _findDll(string rootFolderPath)
    {
        _sourceDllList.Clear();

        if (string.IsNullOrWhiteSpace(rootFolderPath))
        {
            Logger.Warning("Корневая папка для поиска DLL пустая");
            return;
        }

        if (!Directory.Exists(rootFolderPath))
        {
            Logger.Warning($"Корневая папка для поиска DLL не существует: [{rootFolderPath}]");
            return;
        }

        try
        {
            var dllFiles = Directory.GetFiles(rootFolderPath, "*.dll", SearchOption.AllDirectories);
            foreach (string dllPath in dllFiles)
            {
                _sourceDllList.Add(Path.GetFullPath(dllPath));
            }

            Logger.Info($"Найдено DLL в исходной папке: {_sourceDllList.Count}");
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, $"Ошибка поиска DLL в [{rootFolderPath}]");
        }
    }

    public static void CopyDll()
    {
        var stopwatch = Stopwatch.StartNew();
        _parserDllList.Clear();
        _findDll(PathManager.tempDllDir);

        Logger.Info($"Подготовка parsing-кэша из [{PathManager.tempDllDir}]");
        var cleanupStats = _clearFolder(PathManager.parsingDllDir);

        int copied = 0;
        int reused = 0;
        int missing = 0;
        int failed = 0;

        foreach (string sourcePath in _sourceDllList)
        {
            if (!File.Exists(sourcePath))
            {
                missing += 1;
                Logger.Warning($"Источник DLL не найден: [{sourcePath}]");
                continue;
            }

            string fileName = Path.GetFileName(sourcePath);
            var fileInfo = new FileInfo(sourcePath);
            DateTime lastWriteTime = fileInfo.LastWriteTime;
            var cacheFolderName = $"{fileName.Replace(".dll", "")}&{lastWriteTime:yyyyMMddHHmmssfff}";
            string targetDir = Path.Combine(PathManager.parsingDllDir, cacheFolderName);
            Directory.CreateDirectory(targetDir);

            string destPath = Path.Combine(targetDir, fileName);
            try
            {
                File.Copy(sourcePath, destPath, overwrite: false);
                copied += 1;
                Logger.Info($"Новая DLL добавлена в parsing: [{fileName}] -> [{cacheFolderName}]");
            }
            catch (IOException)
            {
                reused += 1;
                Logger.Debug($"DLL уже присутствует в parsing и будет переиспользована: [{fileName}] -> [{cacheFolderName}]");
            }
            catch (Exception ex)
            {
                failed += 1;
                Logger.Exception(ex, $"Не удалось подготовить DLL [{sourcePath}]");
            }

            _parserDllList.Add(destPath);
        }

        _saveDllListToXml(PathManager.tempDllDir);
        stopwatch.Stop();
        Logger.Info(
            $"Подготовка parsing завершена за {stopwatch.ElapsedMilliseconds} ms | source = {_sourceDllList.Count}, copied = {copied}, reused = {reused}, lockedFolders = {cleanupStats.Locked}, removedFolders = {cleanupStats.Removed}, missing = {missing}, failed = {failed}, listed = {_parserDllList.Count}"
        );
    }

    private static void _saveDllListToXml(string folder)
    {
        Logger.Info($"Сохранение путей DLL в XML [{folder}]");
        var xmlFilePath = Path.Combine(folder, "dll_path.xml");

        if (_parserDllList == null || _parserDllList.Count == 0)
        {
            Logger.Warning("Список DLL пустой, xml не будет обновлен");
            return;
        }

        try
        {
            XDocument doc;

            if (File.Exists(xmlFilePath))
            {
                doc = XDocument.Load(xmlFilePath);
                Logger.Debug($"Файл списка DLL уже существует: [{xmlFilePath}]");
            }
            else
            {
                doc = new XDocument(new XElement("root"));
                Logger.Info($"Файл списка DLL не существует, создается новый: [{xmlFilePath}]");
            }

            if (doc.Root == null)
            {
                doc.Add(new XElement("root"));
            }

            var root = doc.Root;
            root.Elements("dll").Remove();

            foreach (string dllPath in _parserDllList)
            {
                root.Add(new XElement("dll", dllPath));
            }

            doc.Save(xmlFilePath);
            Logger.Info($"XML со списком DLL сохранен, записей: {_parserDllList.Count}");
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, $"Ошибка сохранения XML со списком DLL [{xmlFilePath}]");
        }
    }

    public static void LoadDllListFromXml()
    {
        _parserDllList.Clear();
        var xmlFilePath = PathManager.parsingDllListPath;
        Logger.Info($"Чтение списка DLL из файла [{xmlFilePath}]");

        if (string.IsNullOrWhiteSpace(xmlFilePath) || !File.Exists(xmlFilePath))
        {
            Logger.Warning("XML-файл со списком DLL не найден или путь пуст");
            return;
        }

        try
        {
            XDocument doc = XDocument.Load(xmlFilePath);
            if (doc.Root == null)
            {
                Logger.Warning("XML-файл списка DLL пуст или не содержит корневого элемента");
                return;
            }

            int loaded = 0;
            int missing = 0;

            foreach (var element in doc.Root.Elements("dll"))
            {
                string path = element.Value?.Trim();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    _parserDllList.Add(path);
                    loaded += 1;
                    Logger.Debug($"DLL из xml: [{path}]");
                }
                else if (!string.IsNullOrEmpty(path))
                {
                    missing += 1;
                    Logger.Warning($"DLL из xml не найдена: [{path}]");
                }
            }

            Logger.Info($"Список DLL загружен из xml: loaded = {loaded}, missing = {missing}");
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, $"Ошибка загрузки XML со списком DLL [{xmlFilePath}]");
        }
    }

    public static List<string> DllList()
    {
        Logger.Debug($"Актуальный список DLL: {_parserDllList.Count} шт.");
        foreach (var dll in _parserDllList)
        {
            Logger.Debug(dll);
        }
        return _parserDllList;
    }

    private static CleanupStats _clearFolder(string folder)
    {
        Logger.Info($"Очистка папки промежуточных сборок [{folder}]");
        var stats = new CleanupStats();

        try
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                Logger.Info($"Папка промежуточных сборок создана: [{folder}]");
                return stats;
            }

            foreach (string subDir in Directory.GetDirectories(folder))
            {
                try
                {
                    Directory.Delete(subDir, recursive: true);
                    stats.Removed += 1;
                    Logger.Debug($"Удалена старая подпапка: [{subDir}]");
                }
                catch (UnauthorizedAccessException)
                {
                    stats.Locked += 1;
                    Logger.Info($"Подпапка parsing занята и оставлена без удаления: [{Path.GetFileName(subDir)}]");
                }
                catch (IOException)
                {
                    stats.Locked += 1;
                    Logger.Info($"Подпапка parsing используется и оставлена без удаления: [{Path.GetFileName(subDir)}]");
                }
                catch (Exception ex)
                {
                    stats.Failed += 1;
                    Logger.Exception(ex, $"Не удалось удалить подпапку parsing [{subDir}]");
                }
            }

            var total = Directory.GetDirectories(folder).Length;
            Logger.Info($"Очистка parsing завершена | total = {total}, removed = {stats.Removed}, locked = {stats.Locked}, failed = {stats.Failed}");
        }
        catch (Exception ex)
        {
            stats.Failed += 1;
            Logger.Exception(ex, $"Ошибка при очистке директории [{folder}]");
        }

        return stats;
    }

    private class CleanupStats
    {
        public int Removed { get; set; }
        public int Locked { get; set; }
        public int Failed { get; set; }
    }
}
