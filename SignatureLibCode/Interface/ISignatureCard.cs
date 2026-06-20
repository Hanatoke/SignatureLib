using System.Collections.Generic;
using SignatureLib.SignatureLibCode.Core;

namespace SignatureLib.SignatureLibCode.Interface;

public interface ISignatureCard
{
    IEnumerable<SignatureInfo> SignatureInfos { get; }
    bool ShouldAutoAddSignature => true;
}