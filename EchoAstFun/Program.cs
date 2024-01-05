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
        
        var outputDir = "./output/" + Path.GetFileNameWithoutExtension(module.Name) + "/";
        Directory.CreateDirectory(outputDir);
        // foreach (var method in module.GetAllTypes().SelectMany(x => x.Methods))
        {
            if (method is { CilMethodBody: null })
                return;
                // continue;
            
            Console.WriteLine(method.MetadataToken + " " + method);
                
            var cfg = method.CilMethodBody.ConstructStaticFlowGraph();
            var cilPurityClassifier = new CilPurityClassifier();
            var ast = cfg.ToAst(cilPurityClassifier);
            
            // var fileStream = new FileStream(outputDir + "ast_" + method.MetadataToken + ".dot", FileMode.Create);
            // var fileStreamWriter = new StreamWriter(fileStream);
            //
            // ast.ToDotGraph(fileStreamWriter);
            // fileStreamWriter.Close();
            
            var mathAstListener = new MathAstListener(module);
            var mathAstWalker = new AstNodeWalker<CilInstruction>(mathAstListener);
            
            foreach (var ins in ast.Nodes.SelectMany(node => node.Contents.Instructions))
                mathAstWalker.Walk(ins);
        }

        module.Write(outputDir + module.Name);
        Console.WriteLine("Done!");
    }

    public static void mathmutations()
    {
        var a = 0.2613872124741694 + Math.Sqrt(1.5 + Math.Sqrt(36)) - 1; // 2.0
        Console.WriteLine(a);
    }
}