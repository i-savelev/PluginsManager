using System;

namespace PluginsManager
{
    public static class Properties
    {
        public static string Version = "1.0.0";

        // Обычная версия
        public static string AddinPath = @"..\PluginsManager\PluginsManager.addin";
        public static string DllFolder = @"..\PluginsManager\bin\Build\";
        public static string DllFileName = "PluginsManager.dll";

        // Админ версия
        public static string AdminAddinPath = @"..\PluginsManager.AdminTools\PluginsManagerAdminTools.addin";
        public static string AdminDllFolder = @"..\PluginsManager.AdminTools\bin\Build\";
        public static string AdminDllFileName = "PluginsManager.AdminTools.dll";

        public static string SubfolderName = "PluginsManager"; 
        public static string ProjectName = "Plugins Manager";
        public static string Guid = "8BD7D784-31D5-4536-9FE2-416A2DC958B8";
        public static string OutputDir = "..\\";

    }
}