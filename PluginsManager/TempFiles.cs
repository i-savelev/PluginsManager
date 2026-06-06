using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace PluginsManager
{
    public static class TempFiles
    {
        public static void Init()
        {
            Directory.CreateDirectory(PathManager.tempDllDir);
            Logger.Info($"Временная папка плагина: [{PathManager.tempDllDir}]");
        }

        public static void CopyToTemp(string sourceDirectory)
        {
            var stopwatch = Stopwatch.StartNew();
            Logger.Info($"Копирование файлов во временную папку из [{sourceDirectory}]");
            try
            {
                double totalSize = GetDirectorySize(sourceDirectory);
                double totalSizeMb = Math.Round(totalSize / 1024.0 / 1024.0, 2);
                Logger.Info($"Исходная папка: размер = {totalSizeMb:F2} MB");
                if (totalSizeMb > 50)
                {
                    DialogResult result = MessageBox.Show(
                        $"Размер папки {totalSizeMb:F2} MB, хотите ее загрузить?",
                        "Подтверждение",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    Logger.Info($"Подтверждение копирования большой папки: {result}");
                    if (result == DialogResult.Yes)
                    {
                        if (Directory.Exists(PathManager.tempDllDir))
                        {
                            Directory.Delete(PathManager.tempDllDir, recursive: true);
                            Logger.Info($"Удаление предыдущей временной папки [{PathManager.tempDllDir}]");
                        }
                        CopyDirectory(sourceDirectory, PathManager.tempDllDir);
                    }
                }
                else
                {
                    if (Directory.Exists(PathManager.tempDllDir))
                    {
                        Directory.Delete(PathManager.tempDllDir, recursive: true);
                        Logger.Info($"Удаление предыдущей временной папки [{PathManager.tempDllDir}]");
                    }
                    CopyDirectory(sourceDirectory, PathManager.tempDllDir);
                }

                stopwatch.Stop();
                Logger.Info($"Копирование завершено за {stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.Exception(ex, $"Ошибка при копировании во временную папку через {stopwatch.ElapsedMilliseconds} ms");
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
