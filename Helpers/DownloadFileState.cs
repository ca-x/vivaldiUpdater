using System.IO;

namespace VivaldiUpdater.Helpers
{
    internal static class DownloadFileState
    {
        public static void EnsureDirectory(string outputPath)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public static long GetLength(string outputPath)
        {
            return File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0;
        }

        public static void Reset(string outputPath, string metadataPath)
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }
        }
    }
}
