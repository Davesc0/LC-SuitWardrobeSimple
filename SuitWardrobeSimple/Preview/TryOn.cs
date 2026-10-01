using GameNetcodeStuff;

namespace SuitWardrobeSimple
{
    // Local only, no RPC, so nobody else sees the suit being tried on.
    internal static class TryOn
    {
        private static int _original = -1;

        internal static int Original => _original;

        internal static void Begin(PlayerControllerB player)
        {
            _original = player.currentSuitID;
        }

        internal static void Show(PlayerControllerB player, int suitId)
        {
            if (player != null && suitId >= 0 && player.currentSuitID != suitId)
                UnlockableSuit.SwitchSuitForPlayer(player, suitId, playAudio: false);
        }

        internal static void Cancel(PlayerControllerB player)
        {
            if (_original >= 0)
                Show(player, _original);
            _original = -1;
        }

        // Switch back first: SwitchSuitToThis does nothing if the player already wears that suit.
        internal static void Wear(PlayerControllerB player, UnlockableSuit hanger)
        {
            Cancel(player);
            if (hanger != null && player != null)
                hanger.SwitchSuitToThis(player);
        }
    }
}
