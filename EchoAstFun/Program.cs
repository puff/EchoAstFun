using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using Echo.Ast;
using Echo.Ast.Construction;
using Echo.Platforms.AsmResolver;

namespace EchoAstFun;

internal class Program
{
    public static void Main(string[] args)
    {
        // var module = ModuleDefinition.FromFile(args[0]);
        // var method = module.ManagedEntryPointMethod!;
        var module = ModuleDefinition.FromModule(typeof(Program).Module);
        var method = module.TopLevelTypes.Single(x => x.Name == "Program").Methods
            .Single(x => x.Name == "mathmutations");
        
        if (method is { CilMethodBody: null })
            return;

        var cfg = method.CilMethodBody.ConstructStaticFlowGraph();
        var cilPurityClassifier = new CilPurityClassifier();
        var ast = cfg.ToAst(cilPurityClassifier);

        var mathAstListener = new MathAstListener(module);
        var mathAstWalker = new AstNodeWalker<CilInstruction>(mathAstListener);

        foreach (var ins in ast.Nodes.SelectMany(node => node.Contents.Instructions))
            mathAstWalker.Walk(ins);

        Directory.CreateDirectory("./output");
        module.Write("./output/" + module.Name);

        // var fileStream = new FileStream("./output/ast_" + module.Name + ".dot", FileMode.Create);
        // var fileStreamWriter = new StreamWriter(fileStream);
        //
        // ast.ToDotGraph(fileStreamWriter);
        // fileStreamWriter.Close();
    }

    public static void mathmutations()
    {
        var a = 0.2613872124741694 + Math.Sqrt(1.5 + Math.Sqrt(36)); // 3.0
        Console.WriteLine(a);
    }
}