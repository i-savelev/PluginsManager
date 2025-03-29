﻿using System;
using WixSharp;

namespace Build
{
    class Installer
    {
        private static string version = "1.0.0";
        static void Main(string[] args)
        {
            string addin_file_24 = @"c:\Users\Savelev.IA\source\repos\PluginsManager\PluginsManager\IS_PluginsManager.addin";
            string subfolder_name = "IS_PluginsManager";
            string source_dll_folder = @"c:\Users\Savelev.IA\source\repos\PluginsManager\PluginsManager\bin\Build\";
            var feature24 = new Feature("Plugins Manager");

            feature24.Condition = new FeatureCondition("PROP1 = 1", level: 1);

            // Создаем директорию для AppData\Roaming
            var project = new Project("PluginsManager",
                new Dir(@"[AppDataFolder]\Autodesk\Revit\Addins\2024", // <-- Используем [AppDataFolder]
                    new WixSharp.File(feature24, addin_file_24),    
                    new Dir(new Id("SUBFOLDER24"), subfolder_name,
                        new Files(feature24, source_dll_folder + "*.*")
                    )
                )
            );

            project.GUID = new Guid("8BD7D784-31D5-4536-9FE2-416A2DC958B8");
            project.OutFileName = "Plugins manager";
            project.Version = new Version(version);
            project.UI = WUI.WixUI_FeatureTree;
            project.OutDir = "output";
            project.InstallPrivileges = InstallPrivileges.limited;
            try
            {
                Compiler.BuildMsi(project);
                Console.WriteLine("MSI file created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating MSI: {ex.Message}");
            }
        }
    }
}