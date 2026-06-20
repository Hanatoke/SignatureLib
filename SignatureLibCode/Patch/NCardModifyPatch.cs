using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using SignatureLib.SignatureLibCode.Core;
using SignatureLib.SignatureLibCode.Extensions;

namespace SignatureLib.SignatureLibCode.Patch;

public class NCardModifyPatch
{
    public static readonly HashSet<NCard> HasModified = [];
    [HarmonyPatch(typeof(NCard),"Reload")]
        public static class ReloadPatch
        {
            [HarmonyPostfix]
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
                    node.QueueFreeSafelyNoPool();
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(NCard),nameof(NCard.Model), MethodType.Setter)]
        public static class NCardModelSetPatch
        {
            private static readonly MethodInfo UnsubscribeFromModel = AccessTools.Method(typeof(NCard), "UnsubscribeFromModel");
            private static readonly FieldInfo CardOverlay = AccessTools.Field(typeof(NCard), "_cardOverlay");
            [HarmonyPrefix]
            public static bool Prefix(NCard __instance,ref CardModel ____model,CardModel value)
            {

                if (____model != value && HasModified.Contains(__instance))
                {
                    try
                    {
                        HasModified.Remove(__instance);
                        UnsubscribeFromModel.Invoke(__instance, [____model]);
                        //获取模板
                        NCard template=NodePool.Get<NCard>();
                        if (template.Body==null)
                        {
                            template._Ready();
                        }
                        Control control = template.Body;
                        
                        //记录UniqueName
                        Dictionary<NodePath, NodePath> uniqueNames=new();
                        RecordUniqueName(__instance,__instance.Body, uniqueNames);
                        
                        //保留移动vfx
                        foreach (Godot.Node vfx in __instance.CardVfxContainer.GetChildren().ToList())
                        {
                            // RecordPosition(__instance,vfx,positions);
                            vfx.ReparentSafely(template.CardVfxContainer,false);
                        }
                        
                        //移除
                        __instance.Body.Free();
                        CardOverlay.SetValue(__instance,null);
                        
                        control.ReparentSafely(__instance,false);
                        
                        ____model = template.Model;
                        template.QueueFreeSafelyNoPool();
                        
                        //重构UniqueName
                        RecoveryUniqueName(__instance, uniqueNames);
                        
                        __instance._Ready();
                    }
                    catch (Exception e)
                    {
                        SignatureLibMain.Logger.Info(e.ToString());
                    }
                }
                return true;
            }
        }

        private static void RecordUniqueName(Godot.Node root, Godot.Node node, Dictionary<NodePath, NodePath> uniqueNames)
        {
            if (node.UniqueNameInOwner && node.Owner != null)
            {
                uniqueNames[root.GetPathTo(node)] = root.GetPathTo(node.Owner);
            }
            foreach (var child in node.GetChildren())
            {
                RecordUniqueName(root,child, uniqueNames);
            }
        }

        private static void RecoveryUniqueName(Godot.Node root, Dictionary<NodePath, NodePath> uniqueNames)
        {
            foreach (var keyValuePair in uniqueNames)
            {
                if (root.GetNodeOrNull(keyValuePair.Key) is {} node && root.GetNodeOrNull(keyValuePair.Value) is {} owner)
                {
                    node.Owner = owner;
                    node.UniqueNameInOwner = true;
                }
            }
        }
}