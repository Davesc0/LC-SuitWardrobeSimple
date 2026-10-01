using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace SuitWardrobeSimple
{
    // The suit texture is a UV sheet, so thumbnails are photos of the preview, cached on disk.
    internal static class Thumbnails
    {
        internal const int Size = 160;

        private static readonly Dictionary<string, Texture2D> Pictures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> Missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static string Folder => Path.Combine(Paths.CachePath, "SuitWardrobeSimple");

        internal static Texture2D Get(string suit)
        {
            if (Pictures.TryGetValue(suit, out Texture2D picture) && picture != null)
                return picture;
            if (Missing.Contains(suit))
                return null;

            picture = Load(suit);
            if (picture != null)
                Pictures[suit] = picture;
            else
                Missing.Add(suit);
            return picture;
        }

        internal static bool Has(string suit) => Get(suit) != null;

        internal static void Store(string suit, Texture2D picture)
        {
            if (Pictures.TryGetValue(suit, out Texture2D old) && old != null && old != picture)
                UnityEngine.Object.Destroy(old);

            Pictures[suit] = picture;
            Missing.Remove(suit);

            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllBytes(PathOf(suit), picture.EncodeToPNG());
            }
            catch (Exception e)
            {
                Plugin.mlg.LogWarning($"Could not save the picture of {suit}; it is taken again next time: {e.Message}");
            }
        }

        private static Texture2D Load(string suit)
        {
            string path = PathOf(suit);
            if (!File.Exists(path))
                return null;

            try
            {
                var picture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = "SuitWardrobeSimpleThumbnail" };
                if (picture.LoadImage(File.ReadAllBytes(path)))
                    return picture;
                UnityEngine.Object.Destroy(picture);
            }
            catch (Exception e)
            {
                Plugin.mlg.LogWarning($"Could not read the picture of {suit}: {e.Message}");
            }
            return null;
        }

        private static string PathOf(string suit)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            string safe = new string(suit.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
            return Path.Combine(Folder, safe + ".png");
        }
    }
}
