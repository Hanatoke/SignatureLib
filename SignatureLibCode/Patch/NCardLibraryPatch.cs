using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using SignatureLib.SignatureLibCode.Core;

namespace SignatureLib.SignatureLibCode.Patch;

public static class NCardLibraryPatch
{
    [HarmonyPatch(typeof(NCardLibrary), "UpdateFilter")]
    public static class NCardLibraryUpdateFilterPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.MatchStartForward(new CodeMatch((code=>code.StoresField(AccessTools.Field(typeof(NCardLibrary),"_filter")))));
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(NCardLibraryPatch), nameof(Filter))
                );
            return matcher.InstructionEnumeration();
        }
    }

    public static Func<CardModel, bool> Filter(Func<CardModel, bool> filter,NCardLibrary library)
    {
        if (library.FilterHasSignatureTickBox() is { IsTicked: true })
        {
            return card => card.HasSignature() && filter(card);
        }
        return filter;
    }

    public static readonly string FilterHasSignatureTickBoxName = "FilterHasSignatureTickBox";
    public static NLibraryStatTickbox FilterHasSignatureTickBox(this NCardLibrary library)=>library.GetNodeOrNull<NLibraryStatTickbox>("%"+FilterHasSignatureTickBoxName);
    [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
    public static class NCardLibraryReadyPatch
    {
        [HarmonyPostfix]
        public static void _Ready(NCardLibrary __instance)
        {
            var tickbox = NodeHelper.CreateTickbox();
            tickbox.Name = FilterHasSignatureTickBoxName;
            __instance.GetNodeOrNull<VBoxContainer>("Sidebar/MarginContainer/BottomVBox")?.AddChildSafely(tickbox);
            tickbox.Owner = __instance;
            tickbox.UniqueNameInOwner = true;
            tickbox.IsTicked = false;
            tickbox.SetLabel(new LocString("gameplay_ui","SIGNATURELIB-FILTER_SIGNATURE").GetFormattedText());
            tickbox.Connect(NTickbox.SignalName.Toggled, Callable.From((NTickbox _) =>
            {
                AccessTools.Method(typeof(NCardLibrary),"UpdateFilter")?.Invoke(__instance,[false]);
            }));
            Godot.Node last=tickbox.GetIndex()-1<0?tickbox.GetParent():tickbox.GetParent().GetChild(tickbox.GetIndex()-1);
            if (last is Control control)
            {
                control.FocusNeighborBottom = tickbox.GetPath();
                tickbox.FocusNeighborTop=control.GetPath();
            }
        }
    }

    [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuOpened))]
    public static class NCardLibraryOnSubmenuOpenedPatch
    {
        [HarmonyPostfix]
        public static void OnSubmenuOpened(NCardLibrary __instance)
        {
            var box = __instance.FilterHasSignatureTickBox();
            if (box != null) box.IsTicked = false;
        }
    }
}