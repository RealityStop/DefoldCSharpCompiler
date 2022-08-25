using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpLua;

public static class DefoldScriptGenerator {
  public static void WriteDefoldScripts(string outFolder, CSharpCompilation compilation) {
    var classDeclarations =
      compilation.SyntaxTrees.Select<SyntaxTree, (string, IEnumerable<ClassDeclarationSyntax>)>(x =>
        (x.FilePath, x.GetRoot().ChildNodes().OfType<ClassDeclarationSyntax>()));


    foreach (var fileDeclarationsCollection in classDeclarations) {
      foreach (var classDeclaration in fileDeclarationsCollection.Item2) {
        if (IsFlaggedForScript(classDeclaration, compilation)) {
          GenScript(outFolder, fileDeclarationsCollection.Item1, classDeclaration);
        }
      }
    }
  }


  private static void GenScript(string outputFolder, string inputFile, ClassDeclarationSyntax classDeclaration) {
    string classname = classDeclaration.Identifier.ToString();
    if (classname == "GameObjectScript" || classname == "GenGOScriptAttribute")
      return;
    
    var methods = classDeclaration.Members.OfType<MethodDeclarationSyntax>();

    using (var writer = new StreamWriter(Path.Combine(outputFolder, classname) + ".script")) {
      writer.WriteLine($"require \"{Path.GetFileName(outputFolder)}.out\"");

      writer.WriteLine("");
      writer.WriteLine("function init(self)");
      writer.WriteLine($"\tself.script = {classname.ToString()}()");
      if (methods.Any(x => x.Identifier.ToString().Equals("init")))
        writer.WriteLine($"\tself.script:init()");
      writer.WriteLine("end");
      writer.WriteLine("");

      if (methods.Any(x => x.Identifier.ToString().Equals("final"))) {
        writer.WriteLine("function final(self)");
        writer.WriteLine($"\tself.script:final()");
        writer.WriteLine("end");
        writer.WriteLine("");
      }
      
      if (methods.Any(x => x.Identifier.ToString().Equals("update"))) {
        writer.WriteLine("function update(self, dt)");
        writer.WriteLine($"\tself.script:update(dt)");
        writer.WriteLine("end");
        writer.WriteLine("");
      }
      
      if (methods.Any(x => x.Identifier.ToString().Equals("fixed_update"))) {
        writer.WriteLine("function fixed_update(self, dt)");
        writer.WriteLine($"\tself.script:fixed_update(dt)");
        writer.WriteLine("end");
        writer.WriteLine("");
      }
      
      if (methods.Any(x => x.Identifier.ToString().Equals("on_message"))) {
        writer.WriteLine("function on_message(self, message, sender)");
        writer.WriteLine($"\tself.script:on_message(message, sender)");
        writer.WriteLine("end");
        writer.WriteLine("");
      }
      
      if (methods.Any(x => x.Identifier.ToString().Equals("on_input"))) {
        writer.WriteLine("function on_input(self, action_id, action)");
        writer.WriteLine($"\treturn self.script:on_input(action_id, action)");
        writer.WriteLine("end");
        writer.WriteLine("");
      }

      if (methods.Any(x => x.Identifier.ToString().Equals("on_reload"))) {
        writer.WriteLine("function on_reload(self)");
        writer.WriteLine($"\tself.script:on_reload()");
        writer.WriteLine("end");
        writer.WriteLine("");
      }
    }
  }


  public static bool IsFlaggedForScript(ClassDeclarationSyntax syntax, CSharpCompilation compilation) {
    var rootType = compilation.GetTypeByMetadataName(syntax.Identifier.ToString());
    return (TypeIsFlaggedForScript(rootType));
  }


  private static bool TypeIsFlaggedForScript(INamedTypeSymbol type) {
    if (type.GetAttributes().Any(x => x.ToString().Equals("GenGOScriptAttribute")))
      return true;

    if (type.BaseType != null)
      return TypeIsFlaggedForScript(type.BaseType);

    return false;
  }
}
