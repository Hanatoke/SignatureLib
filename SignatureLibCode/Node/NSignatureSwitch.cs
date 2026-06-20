using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using SignatureLib.SignatureLibCode.Core;
using SignatureLib.SignatureLibCode.Extensions;

namespace SignatureLib.SignatureLibCode.Node;

public partial class NSignatureSwitch :Control
{
    public static readonly string Path = "signature_switch.tscn".ScenePath();
    public NButton Left;
    public NButton Right;
    public Label Text;
    public NInspectCardScreen InspectCardScreen;
    public CardModel? Card;
    public List<SignatureInfo>? Infos;
    public SignatureInfo? CurrentInfo;
    public static NSignatureSwitch Create(NInspectCardScreen screen)
    {
        var instantiate = PreloadManager.Cache.GetScene(Path).Instantiate<NSignatureSwitch>();
        instantiate.InspectCardScreen = screen;
        return instantiate;
    }

    public override void _Ready()
    {
        this.Text = GetNode<Label>("Label");
        var duplicate = InspectCardScreen.GetNodeOrNull<NGoldArrowButton>("LeftArrow")?.Duplicate(2);
        Left ??= new NGoldArrowButton();
        var center = Size/2;
        if (Left != null)
        {
            if (duplicate != null)
            {
                foreach (var child in duplicate.GetChildren())
                {
                    child.ReparentSafely(Left,false);
                    if (child is Control { Material: not null } control)
                    {
                        control.Material = (Material)control.Material.Duplicate();
                    }
                }
            }
            this.AddChildSafely(Left);
            Left.Size = new Vector2(48, 48);
            Left.PivotOffset = Left.Size / 2;
            Left.Position = center + new Vector2(-100, 0)-Left.Size/2;
            Left.Connect(NClickableControl.SignalName.Released, Callable.From((Action<NButton>) (_ => OnLeftButtonPressed())));
        }
        duplicate?.QueueFreeSafelyNoPool();
        duplicate = InspectCardScreen.GetNodeOrNull<NGoldArrowButton>("RightArrow")?.Duplicate(2);
        Right ??= new NGoldArrowButton();
        if (Right != null)
        {
            if (duplicate != null)
            {
                foreach (var child in duplicate.GetChildren())
                {
                    child.ReparentSafely(Right,false);
                    if (child is Control { Material: not null } control)
                    {
                        control.Material = (Material)control.Material.Duplicate();
                    }
                }
            }
            this.AddChildSafely(Right);
            Right.Size = new Vector2(48, 48);
            Right.PivotOffset = Right.Size / 2;
            Right.Position = center + new Vector2(100, 0)-Right.Size/2;
            Right.Connect(NClickableControl.SignalName.Released, Callable.From((Action<NButton>) (_ => OnRightButtonPressed())));
        }
        duplicate?.QueueFreeSafelyNoPool();
        this.Connect(Control.SignalName.MouseEntered, Callable.From(OnHover));
        this.Connect(Control.SignalName.MouseExited, Callable.From(OnUnhover));
    }

    public void SetCard(CardModel card)
    {
        this.Card = card;
        Infos = card.GetSignatureInfos().ToList();
        CurrentInfo = card.GetCurrentSignature();
        UpdateVisual();
    }

    public void Clear()
    {
        Card = null;
        Infos = null;
        CurrentInfo = null;
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        Left.Visible=false;
        Right.Visible=false;
        Text.Visible=false;
        HideHoverTip();
        if (Card==null || Infos==null || CurrentInfo==null ||Infos.Count<=0)return;
        var index = Infos.IndexOf(CurrentInfo);
        if (index>=0)
        {
            
            var locString = CurrentInfo.Name?.Invoke();
            string defaultString = $"{index + 1}";
            Text.Visible = Infos.Count>1 || locString!=null;
            Text.Text = locString?.GetFormattedText() ?? defaultString;
            // Left.Visible = index > 0;
            // Right.Visible = index < Infos.Count - 1;
            Left.Visible = Infos.Count > 1;
            Right.Visible = Infos.Count > 1;
        }
        else
        {
            SignatureLibMain.Logger.Warn("current info is not in SignatureInfos, is's should happen!");
        }
    }

    public void SetCurrentInfo(SignatureInfo info)
    {
        CurrentInfo = info;
        UpdateVisual();
        if (Card==null)return;
        Card.SetCurrentSignature(info);
        foreach (var nCard in this.GetTree().GetRoot().GetChildrenRecursive<NCard>())
        {
            if (nCard.Model != null && nCard.Model.Id == Card.Id) 
            {
                nCard.ApplySignature();
            }
        }
    }

    public void OnLeftButtonPressed()
    {
        if (Card==null || Infos==null || CurrentInfo==null || Infos.Count<=0)return;
        SetCurrentInfo(Infos[(Infos.IndexOf(CurrentInfo)-1+Infos.Count)%Infos.Count]);
    }

    public void OnRightButtonPressed()
    {
        if (Card==null || Infos==null || CurrentInfo==null || Infos.Count<=0)return;
        SetCurrentInfo(Infos[(Infos.IndexOf(CurrentInfo)+1+Infos.Count)%Infos.Count]);
    }
    public void OnHover()
    {
        if (!Visible)return;
        ShowHoverTip();
    }

    public void OnUnhover()
    {
        if (!Visible)return;
        HideHoverTip();
    }

    public void ShowHoverTip()
    {
        HideHoverTip();
        var locString = CurrentInfo?.Description?.Invoke();
        if (locString==null)return;
        NHoverTipSet.CreateAndShow(this, new HoverTip(locString), HoverTipAlignment.Left);
    }

    public void HideHoverTip()
    {
        NHoverTipSet.Remove(this);
    }
}