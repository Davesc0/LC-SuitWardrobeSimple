using BepInEx.Configuration;

namespace SuitWardrobeSimple
{
    internal static class Settings
    {
        internal static ConfigEntry<bool> HidePageButtons;
        internal static ConfigEntry<bool> ShowBoots;
        internal static ConfigEntry<bool> UnlockAllSuits;

        internal static void Init(ConfigFile config)
        {
            HidePageButtons = config.Bind("Rack", "HidePageButtons", true,
                "Only matters with TooManySuits. Hides the page number and arrows above the rack, " +
                "the rack stays on the first page. The wardrobe lists every suit anyway.");
            ShowBoots = config.Bind("Rack", "ShowBoots", true,
                "Show the boots under the suit rack.");
            UnlockAllSuits = config.Bind("Suits", "UnlockAllSuits", false,
                "Host only. Unlocks every suit for free, including the ones sold in the store. " +
                "Unlocked suits are saved with the save file, turning this off later doesn't take them back.");
        }
    }
}
