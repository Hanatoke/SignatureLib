using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using SignatureLib.SignatureLibCode.Extensions;
using SignatureLib.SignatureLibCode.Node;

namespace SignatureLib.SignatureLibCode.Core;

public static class NCardHelper
{
    public static readonly FieldInfo PortraitBorder=AccessTools.Field(typeof(NCard), "_portraitBorder");
    public static readonly FieldInfo Portrait=AccessTools.Field(typeof(NCard), "_portrait");
    public static readonly FieldInfo Frame=AccessTools.Field(typeof(NCard), "_frame");
    public static readonly FieldInfo Banner=AccessTools.Field(typeof(NCard), "_banner");
    public static readonly FieldInfo TypePlaque=AccessTools.Field(typeof(NCard), "_typePlaque");
    public static readonly FieldInfo DescriptionLabel=AccessTools.Field(typeof(NCard), "_descriptionLabel");
    public static readonly List<Action<NCard, bool>> SwitchOrigin = [
        ((card, b) => (PortraitBorder.GetValue(card) as Control)?.SetVisible(b)),
        ((card, b) => (Portrait.GetValue(card) as Control)?.SetVisible(b)),
        ((card, b) => (Frame.GetValue(card) as Control)?.SetVisible(b)),
        ((card, b) => (Banner.GetValue(card) as Control)?.SetVisible(b)),
        ((card, b) => (TypePlaque.GetValue(card) as Control)?.SetVisible(b)),
    ];
    //--------------------------------------------------------------------------------------------
    public static readonly string SignatureImgName = "SignatureImg";
    public static TextureRect SignatureImg(this NCard nCard)=>nCard.GetNodeOrNull<TextureRect>("%"+SignatureImgName);
    public static readonly string SignatureTypeLabelName="SignatureTypeLabel";
    public static NinePatchRect SignatureTypeLabel(this NCard nCard)=>nCard.GetNodeOrNull<NinePatchRect>("%"+SignatureTypeLabelName);
    public static readonly string SignatureDescShadowName = "SignatureDescShadow";
    public static NDescShadow SignatureDescShadow(this NCard nCard)=>nCard.GetNodeOrNull<NDescShadow>("%"+SignatureDescShadowName);
    public static bool ShouldModify(NCard nCard) => ShouldModify(nCard.Model);
    public static bool ShouldModify(CardModel card)
    {
        if (card == null) return false;
        return card.HasSignature();
    }
    public static bool AfterReload(NCard nCard)
    {
        if (ShouldModify(nCard))
        {
            if (nCard.SignatureImg()==null)
            {
                CreateSignature(nCard);
            }
            if (nCard.Model?.IsEnableSignature()==true)
            {
                nCard.ShowSignature();
            }
            return true;
        }
        return false;
    }
    /// <summary>
    /// Create Signature Node for NCard
    /// </summary>

    public static void CreateSignature(NCard nCard)
    {
        CreateSignatureImg(nCard);
        CreateSignatureTypeLabel(nCard);
        CreateSignatureDescShadow(nCard);
    }
    /// <summary>
    /// Remove All Signature Node for NCard
    /// </summary>
    /// <param name="nCard"></param>

    public static void RemoveSignature(NCard nCard)
    {
        // HideSignature(nCard);
        foreach (var action in SwitchOrigin)
        {
            action(nCard,true);
        }
        if (DescriptionLabel.GetValue(nCard) is RichTextLabel desc)
        {
            desc.Modulate = new Color(desc.Modulate.R, desc.Modulate.G, desc.Modulate.B, 1);
        }
        Godot.Node node = nCard.SignatureImg();
        if (node!=null)
        {
            node.GetParent()?.RemoveChildSafely(node);
            node.QueueFreeSafely();
        }
        node = nCard.SignatureTypeLabel();
        if (node!=null)
        {
            node.GetParent()?.RemoveChildSafely(node);
            node.QueueFreeSafely();
        }
        node = nCard.SignatureDescShadow();
        if (node!=null)
        {
            node.GetParent()?.RemoveChildSafely(node);
            node.QueueFreeSafely();
        }
    }

    public static void CreateSignatureImg(NCard nCard)
    {
        var rect = new TextureRect();
        nCard.Body.AddChildSafely(rect);
        rect.Name = SignatureImgName;
        rect.Owner = nCard;
        rect.UniqueNameInOwner = true;
        rect.ExpandMode = TextureRect.ExpandModeEnum.FitHeight;
        rect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        rect.MouseFilter = Control.MouseFilterEnum.Ignore;
        if (Frame.GetValue(nCard) is TextureRect frame)
        {
            frame.GetParent().MoveChildSafely(rect, frame.GetIndex());
        }

        rect.Visible = false;
    }

    public static void CreateSignatureTypeLabel(NCard nCard)
    {
        var ninePatchRect = TypePlaque.GetValue(nCard) as NinePatchRect;
        if (ninePatchRect == null)return;
        var duplicate =(NinePatchRect)ninePatchRect.Duplicate((int)Godot.Node.DuplicateFlags.Groups);
        duplicate.Name = SignatureTypeLabelName;
        nCard.Body.AddChildSafely(duplicate);
        ninePatchRect.GetParent().MoveChildSafely(duplicate, ninePatchRect.GetIndex());
        duplicate.Owner = nCard;
        duplicate.UniqueNameInOwner = true;
        duplicate.Position = new Vector2(-30.5f, 175);

        duplicate.Visible = false;
    }

    public static void CreateSignatureDescShadow(NCard nCard)
    {
        var rect = PreloadManager.Cache.GetScene("desc_shadow.tscn".ScenePath()).Instantiate<NDescShadow>();
        nCard.Body.AddChildSafely(rect);
        rect.Name = SignatureDescShadowName;
        rect.Owner = nCard;
        rect.UniqueNameInOwner = true;
        rect.ExpandMode = TextureRect.ExpandModeEnum.FitHeight;
        rect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        rect.MouseFilter = Control.MouseFilterEnum.Ignore;
        rect.Texture = PreloadManager.Cache.GetAsset<Texture2D>("desc_shadow.png".ImagePath());
        rect.Size=rect.Texture.GetSize()/2;
        rect.Position=-rect.Size/2;
        rect.PivotOffset = rect.Size / 2;
        if (DescriptionLabel.GetValue(nCard) is Control desc)
        {
            desc.GetParent().MoveChildSafely(rect, desc.GetIndex());
        }
        rect.Visible = false;
    }
    /// <summary>
    /// Apply Current Signature Setting for the NCard
    /// </summary>

    public static void ApplySignature(this NCard nCard)
    {
        if (nCard.Model == null) return;
        if (nCard.Model.GetCurrentSignature() is {} info)
        {
            Texture2D texture2D = info.ImgTexture;
            var rect = nCard.SignatureImg();
            rect.Texture=texture2D;
            Vector2 size = info.GetSignatureSize(texture2D);
            rect.Size = size;
            rect.Position=- size/2;
            rect.PivotOffset = size/2;
            
        }
    }
    /// <summary>
    /// Show and Apply Current Signature for the NCard
    /// </summary>
    public static void ShowSignature(this NCard nCard)
    {
        if (nCard.Visibility != ModelVisibility.Visible) return;
        foreach (var action in SwitchOrigin)
        {
            action(nCard,false);
        }
        ApplySignature(nCard);
        nCard.SignatureImg()?.SetVisible(true);
        nCard.SignatureTypeLabel()?.SetVisible(true);
        nCard.SignatureDescShadow()?.SetVisible(true);
    }
    public static readonly MethodInfo Reload = AccessTools.Method(typeof(NCard), "Reload");
    /// <summary>
    /// Hide the Signature and Show origin NCard visual
    /// </summary>
    public static void HideSignature(this NCard nCard)
    {
        nCard.FadeDescription(1,ignoreVerify:true);
        Reload.Invoke(nCard, null);
        nCard.SignatureImg()?.SetVisible(false);
        nCard.SignatureTypeLabel()?.SetVisible(false);
        nCard.SignatureDescShadow()?.SetVisible(false);
    }
    /// <summary>
    /// Fade the Signature Description
    /// </summary>
    public static void FadeDescription(this NCard nCard,float targetAlpha=1,float? duration=null,bool ignoreVerify=false)
    {
        if (nCard.Model == null) return;
        if (!ignoreVerify && !nCard.Model?.IsEnableSignature()==true) return;
        if (!nCard.IsNodeReady())
        {
            Action afterReload =null;
            afterReload= () =>
            {
                nCard.Ready -= afterReload;
                if (GodotObject.IsInstanceValid(nCard)&&nCard.IsNodeReady())
                {
                    nCard.FadeDescription(targetAlpha,duration,ignoreVerify);
                }
            };
            nCard.Ready += afterReload;
            return;
        }
        if (nCard.SignatureDescShadow() is { } shadow)
        {
            var desc = (DescriptionLabel.GetValue(nCard) as RichTextLabel);
            var typeLabel = nCard.SignatureTypeLabel();
            if (duration!=null)
            {
                shadow.Tween?.Kill();
                var tween = shadow.CreateTween();
                shadow.Tween= tween;
                tween.SetParallel();
                tween.TweenProperty(shadow, "modulate:a", targetAlpha, duration.Value).SetEase(Tween.EaseType.OutIn);
                tween.TweenProperty(typeLabel, "modulate:a", targetAlpha, duration.Value).SetEase(Tween.EaseType.InOut);
                tween.TweenProperty(desc, "modulate:a", targetAlpha, duration.Value).SetEase(Tween.EaseType.InOut);
            }
            else
            {
                shadow.Modulate = new Color(shadow.Modulate.R, shadow.Modulate.G, shadow.Modulate.B, targetAlpha);
                if (typeLabel!=null)
                {
                    typeLabel.Modulate = new Color(typeLabel.Modulate.R, typeLabel.Modulate.G, typeLabel.Modulate.B, targetAlpha);
                }
                if (desc != null)
                {
                    desc.Modulate = new Color(desc.Modulate.R, desc.Modulate.G, desc.Modulate.B, targetAlpha);
                }
            }
            
        }
    }
    
}