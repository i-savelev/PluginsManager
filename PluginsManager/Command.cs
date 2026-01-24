using System.Drawing;

namespace PluginsManager
{
    public class Command
    {
        public string CmdCode { get; set; }
        public string CmdName { get; set; }
        public string CmdDescription { get; set; }
        public Image CmdImage { get; set; }
        public string DllPath { get; } // ← добавлено

        public Command(string code, string name, string description, Image img, string dllPath)
        {
            CmdCode = code;
            CmdName = name;
            CmdDescription = description;
            CmdImage = img;
            DllPath = dllPath;
        }
    }
}
