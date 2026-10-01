using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace SuitWardrobeSimple
{
    // Not in the cache folder, mod managers clear that.
    internal static class Saved
    {
        [Serializable]
        private sealed class Data
        {
            public string lastTab = string.Empty;
            public List<string> favourites = new List<string>();
        }

        private static string FilePath => Path.Combine(Paths.ConfigPath, "SuitWardrobeSimple.state.json");

        private static Data _data;
        private static HashSet<string> _favourites;

        internal static string LastTab
        {
            get => Load().lastTab;
            set
            {
                if (Load().lastTab == value)
                    return;

                _data.lastTab = value ?? string.Empty;
                Save();
            }
        }

        internal static bool IsFavourite(string suit) => Favourites().Contains(suit);

        internal static void ToggleFavourite(string suit)
        {
            HashSet<string> names = Favourites();
            if (!names.Remove(suit))
                names.Add(suit);

            _data.favourites = names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
            Save();
        }

        private static HashSet<string> Favourites()
        {
            return _favourites ??= new HashSet<string>(Load().favourites, StringComparer.OrdinalIgnoreCase);
        }

        private static Data Load()
        {
            if (_data != null)
                return _data;

            try
            {
                if (File.Exists(FilePath))
                    _data = JsonUtility.FromJson<Data>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Plugin.mlg.LogWarning($"Could not read {FilePath}, starting without favourites: {e.Message}");
            }
            _data ??= new Data();
            _data.favourites ??= new List<string>();
            _data.lastTab ??= string.Empty;
            return _data;
        }

        private static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(_data, true));
            }
            catch (Exception e)
            {
                Plugin.mlg.LogWarning($"Could not write {FilePath}: {e.Message}");
            }
        }
    }
}
