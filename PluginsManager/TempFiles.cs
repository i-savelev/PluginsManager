using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;


namespace PluginsManager
{
    public static class TempFiles
    {
        public static void Init()
        {
            Directory.CreateDirectory(PathManager.tempDllDir); // Убедимся, что папка есть
            Logger.Info("TempFiles", $"Временная папка плагина: [{PathManager.tempDllDir}]");
        }

        public static void CopyToTemp(string sourceDirectory)
        {
            Logger.Info("TempFiles", $"Копирование файлов во временную папку...");
            try
            {
                Logger.Info("CreateTemp", $"Исходная папка: [{sourceDirectory}]");
                double totalSize = GetDirectorySize(sourceDirectory);
                Logger.Info("CreateTemp", $"размер исходной папки: [{totalSize}]");
                double totalSizeMb = Math.Round(totalSize / 1024.0 / 1024.0, 2);
                if (totalSizeMb > 50)
                {
                    DialogResult result = MessageBox.Show(
                        $"Размер папки {totalSizeMb}, хотите ее загрузить?",
                        "Подтверждение",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        if (Directory.Exists(PathManager.tempDllDir))
                        {
                            Directory.Delete(PathManager.tempDllDir, recursive: true);
                            Logger.Info("CreateTemp", $"Папка существует, удаление папки: [{PathManager.tempDllDir}]");
                        }
                        CopyDirectory(sourceDirectory, PathManager.tempDllDir);
                    }
                }
                else
                {
                    if (Directory.Exists(PathManager.tempDllDir))
                    {
                        Directory.Delete(PathManager.tempDllDir, recursive: true);
                        Logger.Info("CreateTemp", $"Папка существует, удаление папки: [{PathManager.tempDllDir}]");
                    }
                    CopyDirectory(sourceDirectory, PathManager.tempDllDir);
                    Logger.Info("CreateTemp", $"Папка скопирована: [{sourceDirectory}]->[{PathManager.tempDllDir}]");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
                Logger.Error("CreateTemp", $"Произошла ошибка: {ex.Message}");
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));

                File.Copy(file, destFile, overwrite: true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(dir));

                CopyDirectory(dir, destSubDir);
            }
        }

        private static long GetDirectorySize(string path)
        {
            long size = 0;

            foreach (string file in Directory.GetFiles(path))
            {
                size += new FileInfo(file).Length;
            }

            foreach (string dir in Directory.GetDirectories(path))
            {
                size += GetDirectorySize(dir);
            }
            return size;
        }
    }
}
