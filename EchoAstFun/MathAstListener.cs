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
    
    private readonly CaptureGroup<Expression<CilInstruction>> _sqrtArgumentsGroup;
    private readonly InstructionExpressionPattern<CilInstruction> _sqrtPattern;

    public MathAstListener(ModuleDefinition module)
    {
        _addArgumentsGroup = new CaptureGroup<Expression<CilInstruction>>("addArguments");
        _addPattern = new InstructionExpressionPattern<CilInstruction>(
            new CilInstructionPattern(CilOpCodes.Add),
            ExpressionPattern.Instruction(new CilInstructionPattern(CilOpCodes.Ldc_R8)),
            ExpressionPattern.Instruction(new CilInstructionPattern(CilOpCodes.Ldc_R8))).CaptureArguments(_addArgumentsGroup);

        var mathRef = module.CorLibTypeFactory.CorLibScope.CreateTypeReference("System", "Math");
        var mathOneDoubleSignature = MethodSignature.CreateStatic(module.CorLibTypeFactory.Double, module.CorLibTypeFactory.Double);

        var sqrtMethod = mathRef.CreateMemberReference("Sqrt", mathOneDoubleSignature);
        _sqrtArgumentsGroup = new CaptureGroup<Expression<CilInstruction>>("sqrtArguments");
        _sqrtPattern = new InstructionExpressionPattern<CilInstruction>(
                new CilInstructionPattern(CilOpCodes.Call, new MemberReferencePattern(sqrtMethod)),
                ExpressionPattern.Instruction(new CilInstructionPattern(CilOpCodes.Ldc_R8)))
            .CaptureArguments(_sqrtArgumentsGroup);
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

        var sqrtMatch = _sqrtPattern.Match(expression);
        if (sqrtMatch.IsSuccess)
        {
            var args = sqrtMatch.GetCaptures(_sqrtArgumentsGroup).Cast<InstructionExpression<CilInstruction>>().ToArray();
            result = Math.Sqrt((double)args[0].Instruction.Operand!);
            goto Exit;
        }

        Exit:
        if (result != null)
        {
            Console.WriteLine(expression + " -> " + result);
            
            // replace instruction (expression.WithInstruction replaces the instance in the expression, which doesn't get replicated to AsmResolver when writing)
            expression.Instruction.ReplaceWith(CilOpCodes.Ldc_R8, result);
            foreach (var arg in expression.Arguments)
                ((InstructionExpression<CilInstruction>)arg).Instruction.ReplaceWithNop();
            expression.Arguments.Clear(); // clear arguments in the expression instance so it works recursively
            
        }
    }
}