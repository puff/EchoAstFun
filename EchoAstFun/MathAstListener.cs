using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using Echo.Ast;
using Echo.Ast.Patterns;

namespace EchoAstFun;

public class MathAstListener : AstNodeListener<CilInstruction>
{
    private readonly CaptureGroup<Expression<CilInstruction>> _addArgumentsGroup;
    private readonly InstructionExpressionPattern<CilInstruction> _addPattern;
    
    private readonly CaptureGroup<Expression<CilInstruction>> _subArgumentsGroup;
    private readonly InstructionExpressionPattern<CilInstruction> _subPattern;
    
    private readonly CaptureGroup<Expression<CilInstruction>> _sqrtArgumentsGroup;
    private readonly InstructionExpressionPattern<CilInstruction> _sqrtPattern;

    private readonly CaptureGroup<Expression<CilInstruction>> _tanhArgumentsGroup;
    private readonly InstructionExpressionPattern<CilInstruction> _tanhPattern;
    
    public MathAstListener(ModuleDefinition module)
    {
        // add(ldc.r8, ldc.r8)
        _addArgumentsGroup = new CaptureGroup<Expression<CilInstruction>>("addArguments");
        _addPattern = new InstructionExpressionPattern<CilInstruction>(
            new CilInstructionPattern(CilOpCodes.Add),
            ExpressionPattern.Instruction(new CilInstructionPattern(CilOpCodes.Ldc_R8)),
            ExpressionPattern.Instruction(new CilInstructionPattern(CilOpCodes.Ldc_R8))).CaptureArguments(_addArgumentsGroup);

        // sub(ldc.r8, ldc.r8)
        _subArgumentsGroup = new CaptureGroup<Expression<CilInstruction>>("subArguments");
        _subPattern = new InstructionExpressionPattern<CilInstruction>(
            new CilInstructionPattern(CilOpCodes.Sub),
            ExpressionPattern.Instruction(new CilInstructionPattern(CilOpCodes.Ldc_R8)),
            ExpressionPattern.Instruction(new CilInstructionPattern(CilOpCodes.Ldc_R8))).CaptureArguments(_subArgumentsGroup);

        
        var mathRef = module.CorLibTypeFactory.CorLibScope.CreateTypeReference("System", "Math");
        var mathOneDoubleSignature = MethodSignature.CreateStatic(module.CorLibTypeFactory.Double, module.CorLibTypeFactory.Double);

        // call(double System.Math::Sqrt(double)
        var sqrtMethod = mathRef.CreateMemberReference("Sqrt", mathOneDoubleSignature);
        _sqrtArgumentsGroup = new CaptureGroup<Expression<CilInstruction>>("sqrtArguments");
        _sqrtPattern = new InstructionExpressionPattern<CilInstruction>(
                new CilInstructionPattern(CilOpCodes.Call, new MemberReferencePattern(sqrtMethod)),
                ExpressionPattern.Instruction(new CilInstructionPattern(CilOpCodes.Ldc_R8)))
            .CaptureArguments(_sqrtArgumentsGroup);
        
        // call(double System.Math::Tanh(double)
        var tanhMethod = mathRef.CreateMemberReference("Tanh", mathOneDoubleSignature);
        _tanhArgumentsGroup = new CaptureGroup<Expression<CilInstruction>>("tanhArguments");
        _tanhPattern = new InstructionExpressionPattern<CilInstruction>(
                new CilInstructionPattern(CilOpCodes.Call, new MemberReferencePattern(tanhMethod)),
                ExpressionPattern.Instruction(new CilInstructionPattern(CilOpCodes.Ldc_R8)))
            .CaptureArguments(_tanhArgumentsGroup);
    }
    
    public override void ExitInstructionExpression(InstructionExpression<CilInstruction> expression)
    {
        object? result = null;
        
        var addMatch = _addPattern.Match(expression);
        if (addMatch.IsSuccess)
        {
            var args = addMatch.GetCaptures(_addArgumentsGroup).Cast<InstructionExpression<CilInstruction>>().ToArray();
            result = (double)args[0].Instruction.Operand! + (double)args[1].Instruction.Operand!;
            goto Exit;
        }

        var subMatch = _subPattern.Match(expression);
        if (subMatch.IsSuccess)
        {
            var args = subMatch.GetCaptures(_subArgumentsGroup).Cast<InstructionExpression<CilInstruction>>().ToArray();
            result = (double)args[0].Instruction.Operand! - (double)args[1].Instruction.Operand!;
            goto Exit;
        }
        
        var sqrtMatch = _sqrtPattern.Match(expression);
        if (sqrtMatch.IsSuccess)
        {
            var args = sqrtMatch.GetCaptures(_sqrtArgumentsGroup).Cast<InstructionExpression<CilInstruction>>().ToArray();
            result = Math.Sqrt((double)args[0].Instruction.Operand!);
            goto Exit;
        }

        var tanhMatch = _tanhPattern.Match(expression);
        if (tanhMatch.IsSuccess)
        {
            var args = tanhMatch.GetCaptures(_tanhArgumentsGroup).Cast<InstructionExpression<CilInstruction>>().ToArray();
            result = Math.Tanh((double)args[0].Instruction.Operand!);
            goto Exit;
        }
        
        Exit:
        if (result is null) 
            return;
        
        Console.WriteLine(expression + " -> " + result);
            
        // replace instruction and nop arguments
        // expression.WithInstruction replaces the instance in the expression, which doesn't get replicated to AsmResolver when writing
        expression.Instruction.ReplaceWith(CilOpCodes.Ldc_R8, result);
        foreach (var arg in expression.Arguments)
            // this check isn't needed in this case since we're only matching instruction expressions in our patterns, but it's good to have anyway
            if (arg is InstructionExpression<CilInstruction> instructionExpression)
                instructionExpression.Instruction.ReplaceWithNop();
            else
                Console.WriteLine("arg not InstructionExpression");
        expression.Arguments.Clear(); // clear arguments in the expression instance so it works recursively
    }
}