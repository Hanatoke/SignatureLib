#nullable enable
using System;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace SignatureLib.SignatureLibCode.Core;

public class SignatureInfo
{
    public string Id;
    public string Img;
    public Vector2 Scale = Vector2.One;
    public Func<LocString>? Name;
    public Func<LocString>? Description;
}