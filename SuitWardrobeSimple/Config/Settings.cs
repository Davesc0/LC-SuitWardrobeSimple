using BepInEx.Configuration;

namespace SuitWardrobeSimple
{
    internal static class Settings
    {
        internal static ConfigEntry<bool> HidePageButtons;
        internal static ConfigEntry<bool> ShowBoots;
        internal static ConfigEntry<bool> UnlockAllSuits;
        internal static ConfigEntry<bool> GroupSmallPacks;
        internal static ConfigEntry<int> SmallPackSize;

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
            GroupSmallPacks = config.Bind("Tabs", "GroupSmallPacks", false,
                "Puts the suits of every mod or author with SmallPackSize suits or fewer under the Other tab, " +
                "instead of a tab of their own.");
            SmallPackSize = config.Bind("Tabs", "SmallPackSize", 1,
                new ConfigDescription("How many suits or fewer count as a small pack, for GroupSmallPacks.",
                    new AcceptableValueRange<int>(1, 50)));
        }
    }
}
