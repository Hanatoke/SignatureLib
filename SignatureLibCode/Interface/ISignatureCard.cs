using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using SignatureLib.SignatureLibCode.Core;

namespace SignatureLib.SignatureLibCode.Interface;
/// <summary>
/// This interface is used for <see cref="CardModel"/> to automatically add Signature
/// </summary>
public interface ISignatureCard
{
    /// <summary>
    /// the Signatures will add to the CardModel
    /// <seealso cref="SignatureInfo"/>
    /// </summary>
    IEnumerable<SignatureInfo> SignatureInfos { get; }
    /// <summary>
    /// if ture then automatically add Signature
    /// </summary>
    bool ShouldAutoAddSignature => true;
    /// <summary>
    /// Auto enable the Signature by id when First Start
    /// <remarks>Only a valid id will be effective</remarks>
    /// </summary>
    string AutoEnabledSignature => null;
}