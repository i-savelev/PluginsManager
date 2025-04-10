﻿using Autodesk.Revit.DB;
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
            var buttonDataFamilyCatalog = new PushButtonData(Const.AppProperties.ButtonName, Const.AppProperties.ButtonName, assemblyPath, Const.AppProperties.AssemblyName);
            var buttonFamilyCatalog = panel.AddItem(buttonDataFamilyCatalog) as PushButton;

            string imageName32 = Const.AppProperties.LargeImage;
            string imageName16 = Const.AppProperties.SmallImage;

            buttonFamilyCatalog.LargeImage = GetImageFromResources(imageName32);
            buttonFamilyCatalog.Image = GetImageFromResources(imageName16);

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

