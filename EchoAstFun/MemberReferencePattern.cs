using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using Echo.Ast.Patterns;

namespace EchoAstFun;

public class MemberReferencePattern(MemberReference memberReference) : Pattern<CilInstruction>
{
    private readonly SignatureComparer _comparer = SignatureComparer.Default;
    
    protected override void MatchChildren(CilInstruction input, MatchResult result)
    {
        if (input.Operand is not MemberReference operand)
            return;

        if (!_comparer.Equals(operand, memberReference))
            result.IsSuccess = false;
    }
}