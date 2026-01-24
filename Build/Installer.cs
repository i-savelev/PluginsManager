using PluginsManager;
using System;
using System.IO;
using WixSharp;
using static WixSharp.Win32;

namespace Build
{
    class Installer
    {
        private static string version = Properties.Version;
        static void Main(string[] args)
        {
            var relativeAddinPath = Properties.AddinPath;
            var relativeDllFolder = Properties.DllFolder;
            var addin_file = Path.GetFullPath(relativeAddinPath);
            var source_dll_folder = Path.GetFullPath(relativeDllFolder);
            var subfolder_name = Properties.SubfolderName;

            var feature21 = new Feature("2021")
            {
                Condition = new FeatureCondition("PROP1 = 1", level: 1)
            };

            var feature22 = new Feature("2022")
            {
                Condition = new FeatureCondition("PROP1 = 1", level: 1)
            };

            var feature23 = new Feature("2023")
            {
                Condition = new FeatureCondition("PROP1 = 1", level: 1)
            };

            var feature24 = new Feature("2024")
            {
                Condition = new FeatureCondition("PROP1 = 1", level: 1)
            };

            var feature25 = new Feature("2025")
            {
                Condition = new FeatureCondition("PROP1 = 1", level: 1)
            };

            var feature26 = new Feature("2026")
            {
                Condition = new FeatureCondition("PROP1 = 1", level: 1)
            };

            var project = new Project(Properties.ProjectName,
                new Dir(@"%AppDataFolder%",
                    new Dir("Autodesk",
                        new Dir("Revit",
                            new Dir("Addins",
                                new Dir("2026",
                                    new WixSharp.File(feature26, addin_file),
                                    new Dir(new Id("SUBFOLDER26"), subfolder_name,
                                        new Files(feature26, source_dll_folder + "*.*")
                                        )
                                    ),
                                new Dir("2025",
                                    new WixSharp.File(feature25, addin_file),
                                    new Dir(new Id("SUBFOLDER25"), subfolder_name,
                                        new Files(feature25, source_dll_folder + "*.*")
                                        )
                                    ),
                                new Dir("2024",
                                    new WixSharp.File(feature24, addin_file),
                                    new Dir(new Id("SUBFOLDER24"), subfolder_name,
                                        new Files(feature24, source_dll_folder + "*.*")
                                        )
                                    ),
                                new Dir("2023",
                                    new WixSharp.File(feature23, addin_file),
                                    new Dir(new Id("SUBFOLDER23"), subfolder_name,
                                        new Files(feature23, source_dll_folder + "*.*")
                                        )
                                    ),
                                new Dir("2022",
                                    new WixSharp.File(feature22, addin_file),
                                    new Dir(new Id("SUBFOLDER22"), subfolder_name,
                                        new Files(feature22, source_dll_folder + "*.*")
                                        )
                                    ),
                                new Dir("2021",
                                    new WixSharp.File(feature21, addin_file),
                                    new Dir(new Id("SUBFOLDER21"), subfolder_name,
                                        new Files(feature21, source_dll_folder + "*.*")
                                    )
                                )
                            )
                        )
                    )
                )
            );

            project.GUID = new Guid(Properties.Guid);
            project.OutFileName = Properties.ProjectName;
            project.Version = new Version(version);
            project.UI = WUI.WixUI_FeatureTree;
            project.OutDir = Properties.OutputDir;
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