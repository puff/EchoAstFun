using AsmResolver.PE.DotNet.Cil;
using Echo.Ast.Patterns;
using EchoAstFun;

public class CilInstructionPattern : Pattern<CilInstruction>
{
    public CilInstructionPattern(CilOpCode opCode)
    {
        OpCode = new LiteralPattern<CilOpCode>(opCode);
    }
    
    public CilInstructionPattern(Pattern<CilOpCode> opCode)
    {
        OpCode = opCode;
    }
    
    public CilInstructionPattern(CilOpCode opCode, params Pattern<object>[] operands)
    {
        OpCode = new LiteralPattern<CilOpCode>(opCode);
        Operand = operands;
    }

    public CilInstructionPattern(Pattern<CilOpCode> opCode, params Pattern<object>[] operands)
    {
        OpCode = opCode;
        Operand = operands;
    }

    private Pattern<CilOpCode> OpCode
    {
        get;
        set;
    }

    private Pattern<object>[]? Operand
    {
        get;
    } = null;
    
    protected override void MatchChildren(CilInstruction input, MatchResult result)
    {
        OpCode.Match(input.OpCode, result);
        if (!result.IsSuccess)
            return;
        
        for (var i = 0; i < Operand?.Length; i++)
        {
            if (Operand[i] is LiteralPattern<object> { Value: MemberReferencePattern mRef })
                mRef.Match(input, result);
            else
                Operand[i].Match(input.Operand!, result);
            if (!result.IsSuccess)
                return;
        }
    }

    public override string ToString()
    {
        return $"{OpCode} {Operand?.Length}";
    }
}