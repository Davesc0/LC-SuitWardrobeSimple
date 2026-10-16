using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SuitWardrobeSimple
{
    // Host only: suits are network objects, so only the host can put them on the rack.
    internal static class SuitUnlocker
    {
        private static readonly MethodInfo Unlock = AccessTools.Method(typeof(StartOfRound), "UnlockShipObject");

        // Checked again and again, because getting fired takes the bought suits away.
        internal static void Tick()
        {
            StartOfRound round = StartOfRound.Instance;
            if (!Settings.UnlockAllSuits.Value || round == null || !round.IsServer || !round.inShipPhase || Unlock == null)
                return;

            List<UnlockableItem> unlockables = round.unlockablesList?.unlockables;
            if (unlockables == null)
                return;

            Terminal terminal = null;
            for (int id = 0; id < unlockables.Count; id++)
            {
                UnlockableItem item = unlockables[id];
                if (item == null || item.unlockableType != 0 || item.suitMaterial == null
                    || item.alreadyUnlocked || item.hasBeenUnlockedByPlayer)
                    continue;

                if (terminal == null)
                    terminal = Object.FindObjectOfType<Terminal>();
                if (terminal == null)
                    return;

                round.BuyShipUnlockableClientRpc(terminal.groupCredits, id);
                Unlock.Invoke(round, new object[] { id });
            }
        }
    }
}
