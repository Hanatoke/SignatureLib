#nullable enable
using System;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using SignatureLib.SignatureLibCode.Extensions;

namespace SignatureLib.SignatureLibCode.Core;
/// <summary>
/// Store a Signature info
/// </summary>

public class SignatureInfo
{
    /// <summary>
    /// Must be Unique for one card 
    /// </summary>
    public string Id;
    /// <summary>
    /// the Signature Image Path
    /// </summary>
    public string Img;
    /// <summary>
    /// the Signature Image scale
    /// </summary>
    public Vector2 Scale = Vector2.One;
    /// <summary>
    /// name of Signature
    /// </summary>
    public Func<LocString>? Name;
    /// <summary>
    /// Description of Signature
    /// </summary>
    public Func<LocString>? Description;
    /// <summary>
    /// The default method to verify Signature image path
    /// <seealso cref="SignatureManager.AddSignature"/>
    /// </summary>
    public virtual bool VerifyImgPath(string path) => ResourceLoader.Exists(path);
    /// <summary>
    /// The default method to load Signature image
    /// <seealso cref="NCardHelper.ApplySignature"/>
    /// </summary>
    public virtual Texture2D ImgTexture => ResourceLoader.Load<Texture2D>(Img);
    /// <summary>
    /// The default method to calculate Signature image Size
    /// <seealso cref="NCardHelper.ApplySignature"/>
    /// </summary>
    public virtual Vector2 GetSignatureSize(Texture2D texture)=>texture.GetSize()*Scale;
    /// <summary>
    /// The Signature Description will auto hide when unhover on target Holder.
    /// <seealso cref="SignatureLib.SignatureLibCode.Patch.NCardHolderPatch.Verify"/>
    /// </summary>
    public virtual bool AutoHideDescriptionWhenUnhover(NCardHolder holder) => holder is NGridCardHolder or NHandCardHolder;
    /// <summary>
    /// It will determine the image used to display the shadow below the card text description.
    /// The Texture2D should be 512px*512px. Default img: SignatureLib/images/desc_shadow.png
    /// <seealso cref="NCardHelper.ApplySignature"/>
    /// </summary>
    public virtual Texture2D? ReplaceDescriptionShadowImg => PreloadManager.Cache.GetAsset<Texture2D>("desc_shadow.png".ImagePath());
}