namespace PluginsManager
{
    public static class Const
    {
        public static class PropertyNames
        {
            public static string Application = "Application";
        }
        public static class DllFields
        {
            public static string Name = "IS_NAME";
            public static string TabName = "IS_TAB_NAME";
            public static string Description = "IS_DESCRIPTION";
            public static string Image = "IS_IMAGE";
        }
        public static class UserConfigFile
        {
            public static string FolderName = "PluginsManager";
            public static string Name = "user_config.xml";
            public static string XmlSettings = "Settings";
            public static string XmlFolderPath = "FolderPath";
            public static string XmlExceptionTabs = "ExceptionTabs";
            public static string XmlPost = "Post";
            public static string DefaultPost = "user";
            public static string ManagerPost = "manager";
        }
        public static class AppProperties
        {
            public static string Guid = "4AB79F62-F346-4AC4-8D98-A1345DA39693";
            public static string PanelName = "Plugins Manager";

            public static string ButtonNamePluginsManager = "Plugins\nManager";
            public static string AssemblyNamePluginsManager = "PluginsManager.PluginManager";
            public static string LargeImagePluginsManager = "PluginsManager.Resources.robot32.png";
            public static string SmallImagePluginsManager = "PluginsManager.Resources.robot16.png";
        }
        public static class CmdConfigFile
        {
            public static string ImageFolderName = "img";
            public static string Name = "commands_config.xml";
            public static string XmlRoot = "Commands";
            public static string XmlCommand = "Command";
            public static string[] XmlCode = { "CmdCode", "Имя проекта.Имя класса" };
            public static string[] XmlTab = { "CmdTab", "Название вкладки" };
            public static string[] XmlName = { "CmdName", "Имя для отображения" };
            public static string[] XmlDescription = { "CmdDescription", "Описание команды" };
            public static string[] XmlImage = { "CmdImage", "image.png" };

        }
    }
}