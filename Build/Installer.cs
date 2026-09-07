using PluginsManager;
using System;
using System.IO;
using System.Linq;
using WixSharp;
using static WixSharp.Win32;

namespace Build
{
    class Installer
    {
        private static string version = Properties.Version;

        static void Main(string[] args)
        {
            // Основные файлы
            var addin_file = Path.GetFullPath(Properties.AddinPath);
            var source_dll_folder = Path.GetFullPath(Properties.DllFolder);
            var subfolder_name = Properties.SubfolderName;

            // Admin Tools файлы
            var admin_addin_file = Path.GetFullPath(Properties.AdminAddinPath);
            var admin_dll_folder = Path.GetFullPath(Properties.AdminDllFolder);
            var admin_dll_file = Path.Combine(admin_dll_folder, Properties.AdminDllFileName);

            // Фича для Admin Tools: 
            // Level = 100 гарантирует, что она НЕ установлена по умолчанию (INSTALLLEVEL по умолчанию = 3),
            // но пользователь сможет явно выбрать её в диалоге WixUI_FeatureTree.
            var adminFeature = new Feature("Admin Tools")
            {
                Condition = new FeatureCondition("1", 100)
            };

            var project = new Project(Properties.ProjectName,
                new Dir(@"%AppDataFolder%",
                    new Dir("Autodesk",
                        new Dir("Revit",
                            new Dir("Addins",
                                // 2026
                                new Dir("2026",
                                    new WixSharp.File(addin_file),
                                    new WixSharp.File(adminFeature, admin_addin_file),
                                    new Dir(new Id("SUBFOLDER26"), subfolder_name,
                                        new Files(source_dll_folder + "*.*"),
                                        new WixSharp.File(adminFeature, admin_dll_file)
                                    )
                                ),
                                // 2025
                                new Dir("2025",
                                    new WixSharp.File(addin_file),
                                    new WixSharp.File(adminFeature, admin_addin_file),
                                    new Dir(new Id("SUBFOLDER25"), subfolder_name,
                                        new Files(source_dll_folder + "*.*"),
                                        new WixSharp.File(adminFeature, admin_dll_file)
                                    )
                                ),
                                // 2024
                                new Dir("2024",
                                    new WixSharp.File(addin_file),
                                    new WixSharp.File(adminFeature, admin_addin_file),
                                    new Dir(new Id("SUBFOLDER24"), subfolder_name,
                                        new Files(source_dll_folder + "*.*"),
                                        new WixSharp.File(adminFeature, admin_dll_file)
                                    )
                                ),
                                // 2023
                                new Dir("2023",
                                    new WixSharp.File(addin_file),
                                    new WixSharp.File(adminFeature, admin_addin_file),
                                    new Dir(new Id("SUBFOLDER23"), subfolder_name,
                                        new Files(source_dll_folder + "*.*"),
                                        new WixSharp.File(adminFeature, admin_dll_file)
                                    )
                                ),
                                // 2022
                                new Dir("2022",
                                    new WixSharp.File(addin_file),
                                    new WixSharp.File(adminFeature, admin_addin_file),
                                    new Dir(new Id("SUBFOLDER22"), subfolder_name,
                                        new Files(source_dll_folder + "*.*"),
                                        new WixSharp.File(adminFeature, admin_dll_file)
                                    )
                                ),
                                // 2021
                                new Dir("2021",
                                    new WixSharp.File(addin_file),
                                    new WixSharp.File(adminFeature, admin_addin_file),
                                    new Dir(new Id("SUBFOLDER21"), subfolder_name,
                                        new Files(source_dll_folder + "*.*"),
                                        new WixSharp.File(adminFeature, admin_dll_file)
                                    )
                                )
                            )
                        )
                    )
                )
            );
            project.Language = "ru-RU"; // Русский язык
            project.WixSourceGenerated += (doc) =>
            {
                // Устанавливаем кодовую страницу для Product
                var product = doc.Root.Descendants("Product").FirstOrDefault();
                product?.SetAttributeValue("Codepage", "1251");

                // Также для Package
                var package = doc.Root.Descendants("Package").FirstOrDefault();
                package?.SetAttributeValue("Codepage", "1251");
            };
            project.LicenceFile = @"license.rtf";
            project.GUID = new Guid(Properties.Guid);
            project.OutFileName = Properties.ProjectName;
            project.Description = "asfasf";
            project.Version = new Version(version); 
            project.UI = WUI.WixUI_FeatureTree;
            project.OutDir = Properties.OutputDir;
            project.InstallPrivileges = InstallPrivileges.limited;
            project.MajorUpgrade = new MajorUpgrade
            {
                Schedule = UpgradeSchedule.afterInstallInitialize,
                DowngradeErrorMessage = "Уже установлена более новая версия [ProductName].",
                AllowSameVersionUpgrades = true // <-- Разрешает перезапись при совпадении версий
            };

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