using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace SuitWardrobeSimple
{
    [BepInPlugin(Plugin.modGUID, Plugin.modName, Plugin.modVersion)]
    [BepInDependency("TooManySuits", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string modGUID = "dev.davesco.SuitWardrobeSimple";
        public const string modName = "SuitWardrobeSimple";
        public const string modVersion = "0.3.0";
        private Harmony _harmony = new Harmony(modGUID);
        internal static ManualLogSource mlg = BepInEx.Logging.Logger.CreateLogSource(modGUID);

        void Awake()
        {
            Settings.Init(Config);

            mlg.LogInfo($"Plugin {modName} is loaded!");
            _harmony.PatchAll();
        }
    }
}
