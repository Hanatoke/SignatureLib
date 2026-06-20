using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using SignatureLib.SignatureLibCode.Core;

namespace SignatureLib.SignatureLibCode.Patch;

public class NCardHolderPatch
{
    [HarmonyPatch(typeof(NCardHolder), "CreateHoverTips")]
    public static class NCardHolderCreateHoverTipsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCardHolder __instance)
        {
            // __instance.CardNode?.FadeDescription(1,0.25f);
        }
    }

    [HarmonyPatch(typeof(NCardHolder), "ClearHoverTips")]
    public static class NCardHolderClearHoverTipsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCardHolder __instance)
        {
            // __instance.CardNode?.FadeDescription(0,0.5f);
        }
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    public static class NCardUpdateVisualsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCard __instance,PileType pileType, CardPreviewMode previewMode)
        {
            // if (previewMode != CardPreviewMode.Normal)
            // {
            //     __instance.FadeDescription(1);
            // }
        }
    }
}