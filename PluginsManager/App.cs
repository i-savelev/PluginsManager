using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace PluginsManager
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
            RibbonPanel panel = application.CreateRibbonPanel(Const.AppProperties.PanelName);
            
            var assemblyPath = Assembly.GetExecutingAssembly().Location;

            var btnDataPluginManager = new PushButtonData(Const.AppProperties.ButtonNamePluginsManager, Const.AppProperties.ButtonNamePluginsManager, assemblyPath, Const.AppProperties.AssemblyNamePluginsManager);
            var btnFamilyCatalog = panel.AddItem(btnDataPluginManager) as PushButton;
            btnFamilyCatalog.LargeImage = GetImageFromResources(Const.AppProperties.LargeImagePluginsManager);
            btnFamilyCatalog.Image = GetImageFromResources(Const.AppProperties.SmallImagePluginsManager);

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

