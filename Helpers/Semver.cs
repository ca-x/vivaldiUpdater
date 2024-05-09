using System;

namespace VivaldiUpdater.Helpers
{
    public static class Semver
    {
        public static int IsBigger(string versionA, string versionB)
        {
            var verA = new Version(versionA);
            var verB = new Version(versionB);
            return verA.CompareTo(verB);
        }
    }
}