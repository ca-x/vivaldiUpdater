using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace VivaldiUpdater.Helpers
{
    internal sealed class ResumeMetadata
    {
        public Uri SourceUri { get; set; }
        public string EntityTag { get; set; }
        public DateTimeOffset? LastModified { get; set; }
        public long? ExpectedLength { get; set; }
        public bool Completed { get; set; }
    }

    internal static class DownloadResumeStore
    {
        public static string GetPath(string outputPath)
        {
            return outputPath + ".resume";
        }

        public static void Save(string path, ResumeMetadata metadata)
        {
            File.WriteAllLines(path, new[]
            {
                "1",
                Encode(metadata.SourceUri?.AbsoluteUri),
                Encode(metadata.EntityTag),
                metadata.LastModified?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
                metadata.ExpectedLength?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                metadata.Completed ? "1" : "0"
            });
        }

        public static ResumeMetadata Load(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var lines = File.ReadAllLines(path);
                if (lines.Length != 6 || lines[0] != "1")
                {
                    return null;
                }

                long expectedLength;
                return new ResumeMetadata
                {
                    SourceUri = new Uri(Decode(lines[1]), UriKind.Absolute),
                    EntityTag = Decode(lines[2]),
                    LastModified = string.IsNullOrEmpty(lines[3])
                        ? (DateTimeOffset?)null
                        : DateTimeOffset.Parse(lines[3], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    ExpectedLength = string.IsNullOrEmpty(lines[4])
                        ? (long?)null
                        : long.TryParse(lines[4], NumberStyles.None, CultureInfo.InvariantCulture, out expectedLength)
                            ? expectedLength
                            : (long?)null,
                    Completed = lines[5] == "1"
                };
            }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is ArgumentException)
            {
                return null;
            }
        }

        private static string Encode(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string Decode(string value)
        {
            return string.IsNullOrEmpty(value)
                ? null
                : Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
    }
}
