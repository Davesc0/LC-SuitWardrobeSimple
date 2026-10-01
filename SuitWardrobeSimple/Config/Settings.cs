using BepInEx.Configuration;

namespace SuitWardrobeSimple
{
    internal static class Settings
    {
        internal static ConfigEntry<bool> HidePageButtons;
        internal static ConfigEntry<bool> ShowBoots;

        internal static void Init(ConfigFile config)
        {
            HidePageButtons = config.Bind("Rack", "HidePageButtons", true,
                "Only matters with TooManySuits. Hides the page number and arrows above the rack, " +
                "the rack stays on the first page. The wardrobe lists every suit anyway.");
            ShowBoots = config.Bind("Rack", "ShowBoots", true,
                "Show the boots under the suit rack.");
        }
    }
}
