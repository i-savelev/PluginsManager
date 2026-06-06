using PluginsManager;
using System;
using static PluginsManager.Const;
using Path = System.IO.Path;

public static class PathManager
{
    public static string userFolderFile = string.Empty;
    public static string userConfigFile = string.Empty;
    public static string tempDllDir = string.Empty;
    public static string sourceDir = string.Empty;
    public static string sourceComandConfigFile = string.Empty;
    public static string tempComandConfigFile = string.Empty;
    public static string parsingDllDir = string.Empty;
    public static string parsingDllListPath = string.Empty;

    public static void Init()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        userFolderFile = Path.Combine(appDataPath, UserConfigFile.FolderName);
        userConfigFile = Path.Combine(userFolderFile, UserConfigFile.Name);
        tempDllDir = Path.Combine(appDataPath, Const.UserConfigFile.FolderName, "temp");
        sourceComandConfigFile = Path.Combine(sourceDir, CmdConfigFile.Name);
        tempComandConfigFile = Path.Combine(tempDllDir, CmdConfigFile.Name);
        parsingDllDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Temp", "PluginsManager", "parsing"
            );
        parsingDllListPath = Path.Combine(tempDllDir, "dll_path.xml");

        Logger.Info($"userFolderFile = [{userFolderFile}]");
        Logger.Info($"userConfigFile = [{userConfigFile}]");
        Logger.Info($"tempDllDir = [{tempDllDir}]");
        Logger.Info($"tempComandConfigFile = [{tempComandConfigFile}]");
        Logger.Info($"parsingDllDir = [{parsingDllDir}]");
        Logger.Info($"parsingDllListPath = [{parsingDllListPath}]");
    }

    public static void SetSourcePath(string _sourcePath)
    {
        sourceDir = _sourcePath;
        sourceComandConfigFile = Path.Combine(sourceDir, CmdConfigFile.Name);
        Logger.Info($"sourceDir = [{sourceDir}]");
        Logger.Info($"sourceComandConfigFile = [{sourceComandConfigFile}]");
    }
}
