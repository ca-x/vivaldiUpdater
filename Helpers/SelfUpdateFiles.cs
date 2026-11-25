using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace VivaldiUpdater.Helpers
{
    internal sealed class SelfUpdateRecoveryException : AggregateException
    {
        internal string BackupDirectory { get; }

        internal SelfUpdateRecoveryException(string backupDirectory, IEnumerable<Exception> errors)
            : base("Could not restore the previous updater. Backups: " + backupDirectory, errors)
        {
            BackupDirectory = backupDirectory;
        }
    }

    internal static class SelfUpdateFiles
    {
        private const string ExecutableName = "VivaldiUpdater.exe";
        private const string WorkDirectoryPrefix = ".vivaldi-self-update-";
        private const string CompletedMarker = "completed";

        public static void Apply(string sourceDirectory, string installDirectory, Action restart)
        {
            var sourcePrefix = Path.GetFullPath(sourceDirectory).TrimEnd(Path.DirectorySeparatorChar) +
                               Path.DirectorySeparatorChar;
            var files = Directory.GetFiles(sourcePrefix, "*", SearchOption.AllDirectories)
                .Select(path => path.Substring(sourcePrefix.Length))
                .Where(path => !string.Equals(Path.GetFileName(path), "settings.json", StringComparison.OrdinalIgnoreCase))
                // Replace the entry point only after the resources have been installed.
                .OrderBy(path => string.Equals(path, ExecutableName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (!files.Contains(ExecutableName, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The update package does not contain " + ExecutableName);
            }

            var workDirectory = Directory.CreateDirectory(
                Path.Combine(installDirectory, WorkDirectoryPrefix + Guid.NewGuid().ToString("N"))).FullName;
            var rollback = new Stack<Action>();
            try
            {
                // Stage on the same volume so replacing a file only requires a rename.
                for (int i = 0; i < files.Length; i++)
                {
                    File.Copy(Path.Combine(sourcePrefix, files[i]), Path.Combine(workDirectory, i + ".new"));
                }

                for (int i = 0; i < files.Length; i++)
                {
                    var destination = Path.Combine(installDirectory, files[i]);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    if (File.Exists(destination))
                    {
                        var backup = Path.Combine(workDirectory, i + ".old");
                        // Windows permits renaming a mapped assembly even when overwriting it fails.
                        File.Move(destination, backup);
                        rollback.Push(() => File.Move(backup, destination));
                    }

                    File.Move(Path.Combine(workDirectory, i + ".new"), destination);
                    rollback.Push(() => File.Delete(destination));
                }

                restart();
            }
            catch (Exception updateError)
            {
                var errors = new List<Exception> { updateError };
                while (rollback.Count > 0)
                {
                    try
                    {
                        rollback.Pop()();
                    }
                    catch (Exception rollbackError)
                    {
                        errors.Add(rollbackError);
                    }
                }

                if (errors.Count > 1)
                {
                    throw new SelfUpdateRecoveryException(workDirectory, errors);
                }

                Finish(workDirectory);
                throw;
            }

            Finish(workDirectory);
        }

        internal static void CleanCompletedUpdates(string installDirectory)
        {
            foreach (var directory in Directory.GetDirectories(installDirectory, WorkDirectoryPrefix + "*"))
            {
                var suffix = Path.GetFileName(directory).Substring(WorkDirectoryPrefix.Length);
                if (Guid.TryParseExact(suffix, "N", out _) &&
                    (File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0 &&
                    File.Exists(Path.Combine(directory, CompletedMarker)))
                {
                    TryClean(directory);
                }
            }
        }

        private static void Finish(string directory)
        {
            // The next startup can retry cleanup if the old process still holds a backup open.
            MarkCompleted(directory);
            TryClean(directory);
        }

        private static void MarkCompleted(string directory)
        {
            try
            {
                File.WriteAllText(Path.Combine(directory, CompletedMarker), "");
            }
            catch (IOException ex) { Debug.WriteLine(ex); }
            catch (UnauthorizedAccessException ex) { Debug.WriteLine(ex); }
        }

        private static void TryClean(string directory)
        {
            try
            {
                var marker = Path.Combine(directory, CompletedMarker);
                foreach (var path in Directory.GetFiles(directory))
                {
                    var extension = Path.GetExtension(path);
                    if ((extension == ".old" || extension == ".new") &&
                        int.TryParse(Path.GetFileNameWithoutExtension(path), out var index) && index >= 0)
                    {
                        File.Delete(path);
                    }
                }

                File.Delete(marker);
                try
                {
                    Directory.Delete(directory);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    if (Directory.Exists(directory))
                    {
                        MarkCompleted(directory);
                    }
                    Debug.WriteLine(ex);
                }
            }
            catch (IOException ex) { Debug.WriteLine(ex); }
            catch (UnauthorizedAccessException ex) { Debug.WriteLine(ex); }
        }
    }
}
