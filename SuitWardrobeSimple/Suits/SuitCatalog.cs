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
                entries.Add(new SuitEntry
                {
                    Id = id,
                    Name = unlockable.unlockableName,
                    Source = SuitSources.SourceOf(unlockable.unlockableName),
                    Hanger = hanger
                });
            }
            return entries;
        }
    }
}
