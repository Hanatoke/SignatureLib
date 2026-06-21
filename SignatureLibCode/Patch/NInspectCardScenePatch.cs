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
    public static readonly string ShowDescriptionTickBoxName = "ShowDescriptionTickBox";
    public static NTickbox ShowDescriptionTickBox(this NInspectCardScreen screen)=>screen.GetNodeOrNull<NTickbox>("%"+ShowDescriptionTickBoxName);
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
        public static void Postfix(NInspectCardScreen __instance,NCard ____card)
        {
            var enableTickbox = NodeHelper.CreateTickbox();
            enableTickbox.Name = EnableSignatureTickboxName;
            __instance.AddChildSafely(enableTickbox);
            enableTickbox.Owner = __instance;
            enableTickbox.UniqueNameInOwner = true;
            enableTickbox.IsTicked = false;
            enableTickbox.Position = new Vector2(350, 700);
            enableTickbox.SetLabel(new LocString("gameplay_ui","SIGNATURELIB-ENABLE_SIGNATURE").GetFormattedText());
            enableTickbox.Disable();

            var showDesc = NodeHelper.CreateTickbox();
            showDesc.Name = ShowDescriptionTickBoxName;
            enableTickbox.AddChildSafely(showDesc);
            showDesc.Owner = __instance;
            showDesc.UniqueNameInOwner = true;
            showDesc.IsTicked = true;
            showDesc.Position += new Vector2(0, 50);
            showDesc.SetLabel(new LocString("gameplay_ui","SIGNATURELIB-SHOW_SIGNATURE").GetFormattedText());
            showDesc.Disable();
            
            var signatureSwitch = NSignatureSwitch.Create(__instance);
            signatureSwitch.Name = SignatureSwitchName;
            enableTickbox.AddChildSafely(signatureSwitch);
            signatureSwitch.Owner = __instance;
            signatureSwitch.UniqueNameInOwner = true;
            signatureSwitch.Position += new Vector2(75, 150);
            signatureSwitch.Visible = false;

            showDesc.Connect(NTickbox.SignalName.Toggled, Callable.From((NTickbox _) =>
            {
                if (____card!=null)
                {
                    ____card.FadeDescription(showDesc.IsTicked ? 1: 0);
                }
            }));
            
            enableTickbox.Connect(NTickbox.SignalName.Toggled, Callable.From(((NTickbox _) =>
            {
                signatureSwitch.Visible=enableTickbox.IsTicked;
                showDesc.Visible = enableTickbox.IsTicked;
                var card = __instance.CurrentCard();
                if (card==null)return;
                card.SetSignatureEnable(enableTickbox.IsTicked);
                signatureSwitch.SetCard(card);
                foreach (var nCard in __instance.GetTree().GetRoot().GetChildrenRecursive<NCard>())
                {
                    if (nCard.Model !=null&&nCard.Model.Id==card.Id)
                    {
                        if (enableTickbox.IsTicked)
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
            __instance.ShowDescriptionTickBox()?.Enable();
        }
    }
    [HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Close))]
    public static class NInspectCardScreenClosePatch
    {
        [HarmonyPostfix]
        public static void Postfix(NInspectCardScreen __instance)
        {
            __instance.EnableSignatureTickbox()?.Disable();
            __instance.ShowDescriptionTickBox()?.Disable();
            __instance.SignatureSwitch()?.Clear();
        }
    }
    [HarmonyPatch(typeof(NInspectCardScreen),"UpdateCardDisplay")]
    public static class UpdateCardDisplayPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NInspectCardScreen __instance)
        {
            if (__instance.ShowDescriptionTickBox() is {} tickbox)
            {
                tickbox.IsTicked = true;
            }
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
                if (__instance.ShowDescriptionTickBox() is {} showDesc)
                {
                    showDesc.Visible = tickbox.IsTicked;
                    showDesc.IsTicked=true;
                }
                if (__instance.SignatureSwitch() is {} signatureSwitch)
                {
                    signatureSwitch.Visible = tickbox.IsTicked;
                    signatureSwitch.SetCard(card);
                }
            }
        }
    }
}