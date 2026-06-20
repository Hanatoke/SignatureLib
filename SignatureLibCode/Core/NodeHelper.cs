using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace SignatureLib.SignatureLibCode.Core;

public class NodeHelper
{
    public static NLibraryStatTickbox CreateTickbox()=>PreloadManager.Cache.GetScene("res://scenes/screens/card_library/card_library_tickbox.tscn")
        .Instantiate<NLibraryStatTickbox>();
}