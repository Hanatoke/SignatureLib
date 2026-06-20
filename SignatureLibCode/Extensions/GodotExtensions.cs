using MegaCrit.Sts2.Core.Nodes;

namespace SignatureLib.SignatureLibCode.Extensions;

public static class GodotExtensions
{
    public static void ReparentSafely(this Godot.Node node, Godot.Node newParent, bool keepGlobalTransform=true)
    {
        if (NGame .IsMainThread())
        {
            node.Reparent(newParent, keepGlobalTransform);
        }else
        {
            node.CallDeferred(Godot.Node.MethodName.Reparent, newParent, keepGlobalTransform);
        }
    }
}