# **How to add Signarure to a card?**

#### **①** The simplest usage for enforcing a dependency on the SignatureLib.</br>

Make your card type implement this interface, and the Signature will be automatically added.
</br>[ISignatureCard.cs](SignatureLibCode/Interface/ISignatureCard.cs) ,[SignatureInfo.cs](SignatureLibCode/Core/SignatureInfo.cs)
```csharp
public DemoCard : CardModel , ISignatureCard
{
    /// You need to return the Signature you want to add. 
    /// Multiple different Signature can be added to the same card, but the IDs of each Signature must be unique
    IEnumerable<SignatureInfo> SignatureInfos =>
        [
            new SignatureInfo
            {
                Id= (string) Your id,
                Img= (string)Your own image path,
                Scale = (Vector2) your image scale
            }
        ];
}
```
There are also **Name** and **Description** as optional parameters
```csharp
public Func<LocString>? Name=>()=>new LocString("your table"," name id");
public Func<LocString>? Description=>()=>new LocString("your table"," description id");
```
___
#### **②** When you want to add Signature for cards in the original game or other mods, you can register as a Signature provider.</br>

Use _SignatureManager.RegisterSignatureInfosProvider()_ to register Signature<br>
[SignatureManager.cs](SignatureLibCode/Core/SignatureManager.cs)
```csharp
SignatureManager.RegisterSignatureInfosProvider(card=>
{
    ///It will be executed on all cards after the game has finished initializing.
    ///so you should fully specify precise conditions, such as checking whether it inherits from a certain abstract class.
    if(card == Your custom condition)
    {
        return[new SignatureInfo()
        {
            Id= (string) Your id,
            Img= (string)Your own image path,
            Scale = (Vector2) your image scale
        }]
    }
    return null;
})
```
___
#### **③** If you don’t want to directly depend on SignatureLib, you can use reflection to check whether it has been loaded, and then register the Signature provider accordingly.<br>

Use _SignatureManager.RegisterSignatureSetsProvider()_ to register Signature without **SignatureInfo**.
```csharp
var asm = AppDomain.CurrentDomain.GetAssemblies()
    .FirstOrDefault(a => a.GetName().Name == "SignatureLib");

if (asm != null)
{
    var type = asm.GetType("SignatureLib.SignatureLibCode.Core.SignatureManager");
    var method = type?.GetMethod("RegisterSignatureSetsProvider",
        BindingFlags.Public | BindingFlags.Static);
    method?.Invoke(null, [(Func<CardModel, IEnumerable<(string,string,Vector2,Func<LocString>,Func<LocString>)>>)(card =>
    {
        //
        if (card.Rarity!=CardRarity.Ancient && card is YourAbstractCardClass abstractCard)
        {
            //the name and description can be null.
            return [((string)signature ID,(string)image path,(Vector2)image scale,(Func<LocString>) name,(Func<LocString>) description)];
        }
        return null;
    })]);
}
```
When there is no directly dependency, there may be issues with the loading order of mods, causing SignatureLib to load later than your mod, which in turn prevents the normal addition of Signature.<br>
You need a timing point earlier than the initialization of the SignatureLib but later than the completion of all mod loads to execute the above statement

###### <br>A solution is provided below

```csharp
[HarmonyPatch(typeof(OneTimeInitialization), nameof(OneTimeInitialization.ExecuteEssential))]
static class InitPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        /// write code this...
        
    }
}
```