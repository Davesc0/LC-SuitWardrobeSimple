using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuitWardrobeSimple
{
    internal sealed class SuitEntry
    {
        internal int Id;
        internal string Name;
        internal string Source;

        // The tab it's listed under: its source, or Other when small packs are grouped.
        internal string Tab;

        internal UnlockableSuit Hanger;
    }

    internal static class SuitCatalog
    {
        internal static List<SuitEntry> Owned()
        {
            List<UnlockableItem> unlockables = StartOfRound.Instance?.unlockablesList?.unlockables;
            var entries = new List<SuitEntry>();
            if (unlockables == null)
                return entries;

            var seen = new HashSet<int>();
            IEnumerable<UnlockableSuit> hangers = Resources.FindObjectsOfTypeAll<UnlockableSuit>()
                .Where(hanger => hanger != null && hanger.IsSpawned)
                .OrderBy(hanger => hanger.syncedSuitID.Value);

            foreach (UnlockableSuit hanger in hangers)
            {
                int id = hanger.syncedSuitID.Value;
                if (id < 0 || id >= unlockables.Count || !seen.Add(id))
                    continue;

                UnlockableItem unlockable = unlockables[id];
                string source = SuitSources.SourceOf(unlockable.unlockableName);
                entries.Add(new SuitEntry
                {
                    Id = id,
                    Name = unlockable.unlockableName,
                    Source = source,
                    Tab = source,
                    Hanger = hanger
                });
            }

            if (Settings.GroupSmallPacks.Value)
                GroupSmallPacks(entries, Settings.SmallPackSize.Value);
            return entries;
        }

        private static void GroupSmallPacks(List<SuitEntry> entries, int size)
        {
            HashSet<string> small = new HashSet<string>(entries
                .Where(entry => entry.Source != SuitSources.Vanilla && entry.Source != SuitSources.Other)
                .GroupBy(entry => entry.Source)
                .Where(pack => pack.Count() <= size)
                .Select(pack => pack.Key));

            foreach (SuitEntry entry in entries)
            {
                if (small.Contains(entry.Source))
                    entry.Tab = SuitSources.Other;
            }
        }
    }
}
