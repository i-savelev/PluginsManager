using PluginsManager;
using System;
using System.IO;
using WixSharp;
using WixSharp.UI.Forms;

namespace Build
{
    class Installer
    {
        private static string version = Properties.Version;

        static void Main(string[] args)
        {
            var addin_file = Path.GetFullPath(Properties.AddinPath);
            var source_dll_folder = Path.GetFullPath(Properties.DllFolder);

            var admin_addin_file = Path.GetFullPath(Properties.AdminAddinPath);
            var admin_dll_folder = Path.GetFullPath(Properties.AdminDllFolder);

            var subfolder_name = Properties.SubfolderName;

            var project = new ManagedProject(Properties.ProjectName,
                new Property("INSTALL_ADMIN", "0"),
                new Dir(@"%AppDataFolder%",
                    new Dir("Autodesk",
                        new Dir("Revit",
                            new Dir("Addins",
                                CreateYearDir("2026", addin_file, source_dll_folder, admin_addin_file, admin_dll_folder, subfolder_name),
                                CreateYearDir("2025", addin_file, source_dll_folder, admin_addin_file, admin_dll_folder, subfolder_name),
                                CreateYearDir("2024", addin_file, source_dll_folder, admin_addin_file, admin_dll_folder, subfolder_name),
                                CreateYearDir("2023", addin_file, source_dll_folder, admin_addin_file, admin_dll_folder, subfolder_name),
                                CreateYearDir("2022", addin_file, source_dll_folder, admin_addin_file, admin_dll_folder, subfolder_name),
                                CreateYearDir("2021", addin_file, source_dll_folder, admin_addin_file, admin_dll_folder, subfolder_name)
                            )
                        )
                    )
                )
            );

            project.GUID = new Guid(Properties.Guid);
            project.UpgradeCode = new Guid(Properties.UpgradeGuid);

            project.MajorUpgrade = new MajorUpgrade
            {
                AllowSameVersionUpgrades = true,
                DowngradeErrorMessage = "A newer version of this product is already installed."
            };

            project.OutFileName = Properties.ProjectName;
            project.Version = new Version(version);
            project.OutDir = Properties.OutputDir;
            project.InstallPrivileges = InstallPrivileges.limited;

            project.ManagedUI = new ManagedUI();
            project.ManagedUI.InstallDialogs.Add<AdminChoiceDialog>()
                                              .Add<ProgressDialog>()
                                              .Add<ExitDialog>();

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

        private static Dir CreateYearDir(string year, string addinFile, string dllFolder, string adminAddinFile, string adminDllFolder, string subfolderName)
        {
            var adminCondition = new Condition("REMOVE = \"ALL\" OR INSTALL_ADMIN = \"1\"");
            var subfolderId = new Id($"SUBFOLDER_{year}");

            return new Dir(year,
                new WixSharp.File(addinFile),
                new WixSharp.File(adminAddinFile) { Condition = adminCondition },

                new Dir(subfolderId, subfolderName,
                    new WixSharp.File(Path.Combine(dllFolder, Properties.DllFileName)),
                    new WixSharp.File(Path.Combine(adminDllFolder, Properties.AdminDllFileName)) { Condition = adminCondition }
                )
            );
        }
    }
}