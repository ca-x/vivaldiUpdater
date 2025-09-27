using System;
using System.IO;
using System.IO.Compression;

namespace VivaldiUpdater.Helpers
{
    public static class Copier
    {
        public static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }
      
        public static void ExtractToDirectory(string zipFilePath, string extractPath)
        {
            try
            {
                Directory.CreateDirectory(extractPath);

                // 使用 FileStream 打开 ZIP 文件
                using (FileStream zipToOpen = new FileStream(zipFilePath, FileMode.Open))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
                            
                            if (!destinationPath.StartsWith(Path.GetFullPath(extractPath), StringComparison.OrdinalIgnoreCase))
                            {
                                throw new IOException("Entry is outside the target directory");
                            }

                            if (entry.FullName.EndsWith("/"))
                            {
                                Directory.CreateDirectory(destinationPath);
                            }
                            else
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                                
                                entry.ExtractToFile(destinationPath, true);
                            }
                        }
                    }
                }

                Console.WriteLine($"Successfully extracted {zipFilePath} to {extractPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting {zipFilePath}: {ex.Message}");
            }
        }
    }
}