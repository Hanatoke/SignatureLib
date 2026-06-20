using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace SignatureLib.SignatureLibCode.Core;

public class HarmonyHelper
{
    [HarmonyPatch(typeof(OneTimeInitialization), nameof(OneTimeInitialization.ExecuteEssential))]
    static class InitPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            Harmony harmony = new(typeof(InitPatch).FullName);
            SignatureLibMain.AfterGameInit(harmony);
        }
    }
}