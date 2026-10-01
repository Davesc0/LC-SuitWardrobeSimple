using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;

namespace SuitWardrobeSimple
{
    // More_Suits names suits after the pngs in "moresuits" folders, so the folder tells the source mod.
    internal static class SuitSources
    {
        internal const string Vanilla = "Vanilla";
        internal const string Other = "Other";

        private static Dictionary<string, string> _packs;

        private static HashSet<string> _vanilla;

        // Has to run in the first Awake, More_Suits adds its suits to the same list in Start.
        internal static void RememberVanilla(StartOfRound round)
        {
            if (_vanilla != null || round.unlockablesList == null)
                return;

            _vanilla = new HashSet<string>(
                round.unlockablesList.unlockables
                    .Where(unlockable => unlockable != null && unlockable.unlockableType == 0)
                    .Select(unlockable => Key(unlockable.unlockableName)),
                StringComparer.Ordinal);
        }

        internal static string SourceOf(string suitName)
        {
            string key = Key(suitName);
            if (Packs().TryGetValue(key, out string pack))
                return pack;
            if (_vanilla != null && _vanilla.Contains(key))
                return Vanilla;
            return Other;
        }

        // More_Suits can add " suit" to the names.
        private static string Key(string suitName)
        {
            string key = (suitName ?? string.Empty).Trim().ToLowerInvariant();
            return key.EndsWith(" suit") ? key.Substring(0, key.Length - 5).TrimEnd() : key;
        }

        // One tab per author, or per package if the author only has one.
        private static Dictionary<string, string> Packs()
        {
            if (_packs != null)
                return _packs;

            _packs = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                var folders = Directory.GetDirectories(Paths.PluginPath, "moresuits", SearchOption.AllDirectories)
                    .Select(folder => (Folder: folder, Package: PackageOf(folder)))
                    .ToList();

                var packagesByAuthor = folders
                    .Where(entry => entry.Package != null)
                    .GroupBy(entry => entry.Package.Author, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key,
                        group => group.Select(entry => entry.Package.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                        StringComparer.OrdinalIgnoreCase);

                foreach ((string folder, Package package) in folders)
                {
                    string tab = package == null ? Other
                        : packagesByAuthor[package.Author] > 1 ? Readable(package.Author)
                        : Readable(package.Name);

                    foreach (string file in Directory.GetFiles(folder, "*.png"))
                    {
                        string key = Key(Path.GetFileNameWithoutExtension(file));
                        if (!_packs.ContainsKey(key))
                            _packs[key] = tab;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.mlg.LogWarning($"Could not read the suit folders, every modded suit goes under {Other}: {e.Message}");
            }
            return _packs;
        }

        private sealed class Package
        {
            internal string Author;
            internal string Name;
        }

        private static Package PackageOf(string suitFolder)
        {
            string relative = suitFolder.Substring(Paths.PluginPath.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string folder = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            if (folder.Equals("moresuits", StringComparison.OrdinalIgnoreCase))
                return null;

            int dash = folder.IndexOf('-');
            return dash > 0 && dash < folder.Length - 1
                ? new Package { Author = folder.Substring(0, dash), Name = folder.Substring(dash + 1) }
                : new Package { Author = folder, Name = folder };
        }

        private static string Readable(string name) => name.Replace('_', ' ').Trim();
    }
}
