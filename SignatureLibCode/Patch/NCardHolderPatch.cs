using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using SignatureLib.SignatureLibCode.Core;

namespace SignatureLib.SignatureLibCode.Patch;

public class NCardHolderPatch
{
    public static bool Verify(NCardHolder holder,out bool result)
    {
        if (holder.CardNode?.Model?.IsEnableSignature() == true &&
            holder.CardNode?.Model?.GetCurrentSignature() is { } i )
        {
            result = i.AutoHideDescriptionWhenUnhover(holder);
            return  true;
        }
        result = false;
        return false;
    }
    [HarmonyPatch(typeof(NCardHolder), "CreateHoverTips")]
    
    public static class NCardHolderCreateHoverTipsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCardHolder __instance)
        {
            if (Verify(__instance, out var result)&&result)
            {
                __instance.CardNode?.FadeDescription(1,0.25f);
            }
        }
    }
    [HarmonyPatch(typeof(NHandCardHolder),nameof(NHandCardHolder.BeginDrag))]
    public static class BeginDragPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NHandCardHolder __instance)
        {
            NCardHolderCreateHoverTipsPatch.Postfix(__instance);
        }
    }

    [HarmonyPatch(typeof(NCardHolder), "ClearHoverTips")]
    
    public static class NCardHolderClearHoverTipsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCardHolder __instance)
        {
            if (Verify(__instance, out var result)&&result)
            {
                __instance.CardNode?.FadeDescription(0,0.5f);
            }
        }
    }
    [HarmonyPatch(typeof(NHandCardHolder),nameof(NHandCardHolder.CancelDrag))]
    public static class CancelDragPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NHandCardHolder __instance)
        {
            NCardHolderClearHoverTipsPatch.Postfix(__instance);
        }
    }
    [HarmonyPatch(typeof(NCardHolder), nameof(NCardHolder.ReassignToCard))]
    public static class ReassignToCardPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCardHolder __instance)
        {
            if (Verify(__instance, out var result))
            {
                __instance.CardNode?.FadeDescription(result ? 0 : 1);
            }
        }
    }
    [HarmonyPatch(typeof(NCardHolder), nameof(NCardHolder.CardNode), MethodType.Setter)]
    public static class SetCardPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCardHolder __instance,NCard value)
        {
            if (Verify(__instance, out var result))
            {
                value?.FadeDescription(result ? 0 : 1);
            }
        }
    }
    [HarmonyPatch(typeof(NCardHolder),"OnChildExitingTree")]
    public static class OnChildExitingTreePatch
    {
        [HarmonyPrefix]
        public static void Prefix(NCardHolder __instance,Godot.Node node)
        {
            if (node!=__instance.CardNode || node?.GetParent() == __instance)return;
            __instance.CardNode?.FadeDescription(1);
        }
    }
}