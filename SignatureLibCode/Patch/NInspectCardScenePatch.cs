using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using SignatureLib.SignatureLibCode.Core;
using SignatureLib.SignatureLibCode.Node;

namespace SignatureLib.SignatureLibCode.Patch;

public static class NInspectCardScenePatch
{
    public static readonly string EnableSignatureTickboxName = "EnableSignatureTickbox";
    public static NTickbox EnableSignatureTickbox(this NInspectCardScreen screen)=>screen.GetNodeOrNull<NTickbox>("%"+EnableSignatureTickboxName);
    public static readonly string SignatureSwitchName = "SignatureSwitch";
    public static NSignatureSwitch SignatureSwitch(this NInspectCardScreen screen)=>screen.GetNodeOrNull<NSignatureSwitch>("%"+SignatureSwitchName);
    public static CardModel CurrentCard(this NInspectCardScreen screen)
    {
        var list = AccessTools.Field(typeof(NInspectCardScreen), "_cards").GetValue(screen) as List<CardModel>;
        var index = AccessTools.Field(typeof(NInspectCardScreen), "_index").GetValue(screen) as int?;
        if (list!=null && index!=null)
        {
            return list[index.Value];
        }
        return null;
    }
    [HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen._Ready))]
    public static class NInspectCardScreenReadyPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NInspectCardScreen __instance)
        {
            var duplicate = NodeHelper.CreateTickbox();
            duplicate.Name = EnableSignatureTickboxName;
            __instance.AddChildSafely(duplicate);
            duplicate.Owner = __instance;
            duplicate.UniqueNameInOwner = true;
            duplicate.IsTicked = false;
            duplicate.Position = new Vector2(350, 700);
            duplicate.SetLabel(new LocString("gameplay_ui","SIGNATURELIB-ENABLE_SIGNATURE").GetFormattedText());
            duplicate.Disable();
            
            var signatureSwitch = NSignatureSwitch.Create(__instance);
            signatureSwitch.Name = SignatureSwitchName;
            duplicate.AddChildSafely(signatureSwitch);
            signatureSwitch.Owner = __instance;
            signatureSwitch.UniqueNameInOwner = true;
            signatureSwitch.Position += new Vector2(75, 100);
            signatureSwitch.Visible = false;
            duplicate.Connect(NTickbox.SignalName.Toggled, Callable.From(((NTickbox _) =>
            {
                signatureSwitch.Visible=duplicate.IsTicked;
                var card = __instance.CurrentCard();
                card.SetSignatureEnable(duplicate.IsTicked);
                signatureSwitch.SetCard(card);
                foreach (var nCard in __instance.GetTree().GetRoot().GetChildrenRecursive<NCard>())
                {
                    if (nCard.Model !=null&&nCard.Model.Id==card.Id)
                    {
                        if (duplicate.IsTicked)
                        {
                            nCard.ShowSignature();
                        }
                        else
                        {
                            nCard.HideSignature();
                        }
                    }
                }
            })));
        }
    }
    [HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Open))]
    public static class NInspectCardScreenOpenPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NInspectCardScreen __instance)
        {
            __instance.EnableSignatureTickbox()?.Enable();
        }
    }
    [HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Close))]
    public static class NInspectCardScreenClosePatch
    {
        [HarmonyPostfix]
        public static void Postfix(NInspectCardScreen __instance)
        {
            __instance.EnableSignatureTickbox()?.Disable();
            __instance.SignatureSwitch()?.Clear();
        }
    }

    [HarmonyPatch(typeof(NInspectCardScreen), "SetCard")]
    public static class NInspectCardScreenSetCardPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NInspectCardScreen __instance,int index,List<CardModel> ____cards)
        {
            CardModel card = ____cards[index];
            if (__instance.EnableSignatureTickbox() is {} tickbox)
            {
                tickbox.Visible = card.HasSignature();
                tickbox.IsTicked = card.IsEnableSignature();
                if (__instance.SignatureSwitch() is {} signatureSwitch)
                {
                    signatureSwitch.Visible = tickbox.IsTicked;
                    signatureSwitch.SetCard(card);
                }
            }
        }
    }
}