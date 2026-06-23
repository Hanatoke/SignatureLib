using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using SignatureLib.SignatureLibCode.Core;

namespace SignatureLib.SignatureLibCode.Patch;

public class NCardModifyPatch
{
    public static readonly HashSet<NCard> HasModified = [];
    [HarmonyPatch(typeof(NCard),"Reload")]
        public static class ReloadPatch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            public static void Postfix(NCard __instance)
            {
                if (!__instance.IsNodeReady())
                {
                    return;
                }

                if (NCardHelper.AfterReload(__instance)) 
                {
                    HasModified.Add(__instance);
                }
            }
        }
        //-----------------------------------------------------------------------------------------------
        [HarmonyPatch(typeof(GodotTreeExtensions),nameof(GodotTreeExtensions.QueueFreeSafely))]
        public static class QueueFreeSafelyPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(Godot.Node node)
            {
                if (GodotObject.IsInstanceValid(node) && node is NCard nCard && HasModified.Contains(nCard))
                {
                    HasModified.Remove(nCard);
                    NCardHelper.RemoveSignature(nCard);
                    // nCard.QueueFreeSafelyNoPool();
                    // return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(NCard),nameof(NCard.Model), MethodType.Setter)]
        public static class NCardModelSetPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(NCard __instance,ref CardModel ____model,CardModel value)
            {

                if (____model != value && HasModified.Contains(__instance))
                {
                    // __instance.HideSignature();
                    HasModified.Remove(__instance);
                    NCardHelper.RemoveSignature(__instance);
                }
                return true;
            }
        }
}