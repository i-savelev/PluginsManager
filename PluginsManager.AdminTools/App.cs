using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace PluginsManager.AdminTools
{
    internal class App : IExternalApplication
    {
        private static AddInId addinId = new AddInId(new Guid(Const.AppProperties.Guid));

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            if (application.GetRibbonPanels().Any(panel => panel.Name == Const.AppProperties.PanelName))
            {
                Autodesk.Revit.UI.RibbonPanel exist_panel = application.GetRibbonPanels().FirstOrDefault(panel => panel.Name == Const.AppProperties.PanelName);
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var btnDataAllCommandsInFolder = new PushButtonData(Const.AppProperties.ButtonNameCommandinFolder, Const.AppProperties.ButtonNameCommandinFolder, assemblyPath, Const.AppProperties.AssemblyNameCommandinFolder);
                var btnAllCommandsInFolder = exist_panel.AddItem(btnDataAllCommandsInFolder) as PushButton;
                btnAllCommandsInFolder.LargeImage = GetImageFromResources(Const.AppProperties.LargeImageCommandinFolder);
                btnAllCommandsInFolder.Image = GetImageFromResources(Const.AppProperties.SmallImageCommandinFolder);
            }
            else
            {

                Autodesk.Revit.UI.RibbonPanel new_panel = application.CreateRibbonPanel(Const.AppProperties.PanelName);
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var btnDataAllCommandsInFolder = new PushButtonData(Const.AppProperties.ButtonNameCommandinFolder, Const.AppProperties.ButtonNameCommandinFolder, assemblyPath, Const.AppProperties.AssemblyNameCommandinFolder);
                var btnAllCommandsInFolder = new_panel.AddItem(btnDataAllCommandsInFolder) as PushButton;
                btnAllCommandsInFolder.LargeImage = GetImageFromResources(Const.AppProperties.LargeImageCommandinFolder);
                btnAllCommandsInFolder.Image = GetImageFromResources(Const.AppProperties.SmallImageCommandinFolder);
            }
            return Result.Succeeded;
        }
        public BitmapImage GetImageFromResources(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream($"{resourceName}"))
            {
                if (stream is null)
                {
                    throw new ArgumentException($"Ресурс '{resourceName}' не найден.");
                }

                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
        }
    }
}

