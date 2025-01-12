﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpLua;

public class DefoldScriptGenerator {
    private CSharpCompilation _compilation;


    public void WriteDefoldScripts(string outFolder, CSharpCompilation compilation) {

        Console.WriteLine("Generating script files...");
        
        _compilation = compilation;
        
        var classDeclarations =
            compilation.SyntaxTrees.Select<SyntaxTree, (string, SemanticModel, IEnumerable<(INamedTypeSymbol, ClassDeclarationSyntax)>)>(x =>
                (x.FilePath, _compilation.GetSemanticModel(x) , FetchClassDeclarations(_compilation.GetSemanticModel(x), x)));


        foreach (var fileDeclarationsCollection in classDeclarations) {
            foreach (var declaration in fileDeclarationsCollection.Item3) {
                if (IsFlaggedForScript(declaration.Item1, out string scriptExtension)) {
                    GenScript(outFolder, fileDeclarationsCollection.Item1, fileDeclarationsCollection.Item2, declaration.Item1, declaration.Item2, scriptExtension);
                }
            }
        }
    }




    private static IEnumerable<(INamedTypeSymbol, ClassDeclarationSyntax)> FetchClassDeclarations(SemanticModel model, SyntaxTree tree) {
        var childNodes = tree.GetRoot().ChildNodes();
        var directClasses = childNodes.OfType<ClassDeclarationSyntax>()
            .Select(x => (model.GetDeclaredSymbol(x),x));
        
        var namespaceClasses = childNodes.OfType<NamespaceDeclarationSyntax>()
            .SelectMany(x =>
                x.ChildNodes().OfType<ClassDeclarationSyntax>().Select(y=> (model.GetDeclaredSymbol(y),y)));
        return directClasses.Concat(namespaceClasses);
    }


    private void GenScript(string originalOutputFolder, string inputFile, SemanticModel model,
        INamedTypeSymbol typeSymbol, ClassDeclarationSyntax classDeclaration, string scriptExtension) {
        string classname = typeSymbol.MetadataName;
        string fullQualifiedName = typeSymbol.MetadataName;
        string outputFolder = originalOutputFolder;

        if (typeSymbol.ContainingNamespace != null && !string.IsNullOrEmpty(typeSymbol.ContainingNamespace.Name)) {
            fullQualifiedName = $"{typeSymbol.ContainingNamespace.Name}.{typeSymbol.Name}";
            outputFolder = Path.Combine(originalOutputFolder, typeSymbol.ContainingNamespace.Name);
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);
        }

        var doNotGenerateAttr =
            typeSymbol.GetAttributes()
                .FirstOrDefault(x => x.AttributeClass.ToString().Contains("DoNotGenerate"));

        if (doNotGenerateAttr != null)
            return;

        var methods = classDeclaration.Members.OfType<MethodDeclarationSyntax>();

        using (var writer =
               new StreamWriter(Path.Combine(outputFolder, classname) + "." + scriptExtension)) {
            writer.WriteLine($"require \"{Path.GetFileName(originalOutputFolder)}.out\"");

            GenerateProperties(writer, model, typeSymbol, classDeclaration);



            writer.WriteLine("");
            writer.WriteLine("function init(self)");
            writer.WriteLine($"\tself.script = {fullQualifiedName.ToString()}()");
            writer.WriteLine($"\tself.script:AssignProperties(self)");
            writer.WriteLine(
                $"\tsupport.Component.Register(self.script.LocatorUrl, self.script, {fullQualifiedName.ToString()})");

            if (methods.Any(x => x.Identifier.ToString().Equals("init")))
                writer.WriteLine($"\tself.script:init()");
            writer.WriteLine("end");
            writer.WriteLine("");

            writer.WriteLine("function final(self)");
            writer.WriteLine($"\tself.script:final()");
            writer.WriteLine("end");
            writer.WriteLine("");

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
                writer.WriteLine("function on_message(self, message_id, message, sender)");
                writer.WriteLine($"\tself.script:on_message(message_id, message, sender)");
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

        Console.WriteLine($"\t{classname}.{scriptExtension}");
    }


    private void GenerateProperties(StreamWriter writer, SemanticModel model, INamedTypeSymbol typeSymbol, ClassDeclarationSyntax classDeclaration) {

        //First, find the base type that has the markup
        var genericBasetypes = this.GetBaseTypes(typeSymbol)
            .Where(x=>x.GetAttributes().Any(x=>x.AttributeClass.ToString().EndsWith("GenScriptAttribute")))
            //.Where(x => x.Name.StartsWith("GameObjectScript",StringComparison.OrdinalIgnoreCase)||x.Name.StartsWith("GUIScript", StringComparison.OrdinalIgnoreCase))
            .Cast<INamedTypeSymbol>().Where(x => x.IsGenericType);

        if (genericBasetypes.Count() != 1)
            throw new InvalidOperationException($"Unable to generate Properties for {classDeclaration.Identifier.ToString()}Generic base count is {genericBasetypes.Count()}");

        typeSymbol = genericBasetypes.First();
        typeSymbol =(INamedTypeSymbol)typeSymbol.TypeArguments.First();

        var fields = GetBaseTypes(typeSymbol).SelectMany(x => x.GetMembers()).Where(x => x.Kind == SymbolKind.Field)
            .Cast<IFieldSymbol>();

        foreach (var fieldSymbol in fields) {
            //var defoldProperty =
            //    fieldSymbol.GetAttributes()
            //        .FirstOrDefault(x => x.AttributeClass.ToString().EndsWith("DefoldPropertyAttribute"));

            HandleProperty(writer, /*defoldProperty*/ null, fieldSymbol);
        }
    }

    private static void HandleProperty(StreamWriter writer, AttributeData DefoldProperty, IFieldSymbol fieldSymbol)
    {
        //If there is a specified attribute argument, use that.
        if ((DefoldProperty?.ConstructorArguments.Length ?? 0) >= 1) {
            ConstructPropertyFromAttribute(writer, DefoldProperty, fieldSymbol);
            return;
        }

        //Otherwise look for literals.
        var potentialLiterals = fieldSymbol.GetDeclaringSyntaxNode().ChildNodes().OfType<EqualsValueClauseSyntax>()
            .FirstOrDefault()
            ?.ChildNodes().FirstOrDefault();


        if (potentialLiterals != null)
            if (ConstructFromLiteral(writer, fieldSymbol, potentialLiterals)) {
                return;
            }

        throw new ArgumentException(
            $"DefoldProperty has no initializer or attribute parameter: {fieldSymbol.ToString()}\r\n{fieldSymbol.GetDeclaringSyntaxNode().GetLocationString()}");
    }


    private static bool ConstructFromLiteral(StreamWriter writer, IFieldSymbol fieldSymbol, SyntaxNode potentialLiterals) {
        Func<SyntaxNode, (bool, string)> formatter = x => (true, x.ToString());
        
        List<SyntaxKind> allowedKinds = new List<SyntaxKind>();
        switch (fieldSymbol.Type.MetadataName) {
            case "Int8":
            case "Int16":
            case "Int32":
            case "Int64":
            case "Single":
            case "Double":
                allowedKinds.Add(SyntaxKind.NumericLiteralExpression);
                break;
            case "Boolean":
                allowedKinds.Add(SyntaxKind.FalseLiteralExpression); 
                allowedKinds.Add(SyntaxKind.TrueLiteralExpression);
                break;
            case "Hash":
                allowedKinds.Add(SyntaxKind.StringLiteralExpression);
                allowedKinds.Add(SyntaxKind.ObjectCreationExpression);
                formatter = ParseHashInitializer;
                break;
            case "Url":
                allowedKinds.Add(SyntaxKind.StringLiteralExpression);
                allowedKinds.Add(SyntaxKind.ObjectCreationExpression);
                formatter = ParseUrlInitializer;
                break;
            case "Vector2":
                allowedKinds.Add(SyntaxKind.ObjectCreationExpression);
                formatter = node => { return ParseVector2Initializer(node); };
                break;
            case "Vector3":
                allowedKinds.Add(SyntaxKind.ObjectCreationExpression);
                formatter = node => { return ParseVector3Initializer(node); };
                break;
            case "Vector4":
                allowedKinds.Add(SyntaxKind.ObjectCreationExpression);
                formatter = node => { return ParseVector4Initializer(node); };
                break;
            case "Quaternion":
                allowedKinds.Add(SyntaxKind.ObjectCreationExpression);
                formatter = node => { return ParseQuaternionInitializer(node); };
                break;
            
            //TODO: Resource
            case "Resource":
                allowedKinds.Add(SyntaxKind.ObjectCreationExpression);
                formatter = node => { return ParseQuaternionInitializer(node); };
                break;
        }

        try {
            if (allowedKinds.Any(x=>potentialLiterals.IsKind(x))) {
                var (success, outstring) = formatter(potentialLiterals);
                if (success)
                    writer.WriteLine(@$"go.property(""{fieldSymbol.Name}"", {outstring})");
                else {
                    throw new ArgumentException(
                        $"Unable to convert [DefoldProperty] to script property (attribute or initializer did not resolve): : {fieldSymbol.ToString()}\r\n{fieldSymbol.GetDeclaringSyntaxNode().GetLocationString()}");
                }
                
                return true;
            }
        } catch (Exception e) {
            throw new ArgumentException(
                $"Encountered error while converting [DefoldProperty] to script property : {fieldSymbol.ToString()}\r\n{fieldSymbol.GetDeclaringSyntaxNode().GetLocationString()} \r\n{e.Message}\r\n{e.StackTrace}");
            throw;
        }


        return false;
    }


    private static (bool, string) ParseQuaternionInitializer(SyntaxNode node)
    {
        if (node.IsKind(SyntaxKind.ObjectCreationExpression))
        {
            var str = node.ToString();
            Regex r = new Regex(@"new Quaternion\((\d+)(?:,(\d+))?(?:,(\d+))?(?:,(\d+))?\)");
            var res = r.Match(str);
            if (res.Success)
            {
                if (res.Groups[4].Success)
                    return (true, $"vmath.quat({res.Groups[1]},{res.Groups[2]}, {res.Groups[3]}, {res.Groups[4]})");
                if (res.Groups[1].Success)
                    return (true, $"vmath.quat({res.Groups[1]})");
                return (true, $"vmath.quat()");
            }
        }

        return (false, node.ToString());
    }
    
    private static (bool, string) ParseVector4Initializer(SyntaxNode node)
    {
        if (node.IsKind(SyntaxKind.ObjectCreationExpression))
        {
            var str = node.ToString();
            Regex r = new Regex(@"new Vector4\((\d+)?(?:,(\d+))?(?:,(\d+))?(?:,(\d+))?\)");
            var res = r.Match(str);
            if (res.Success)
            {
                if (res.Groups[4].Success)
                    return (true, $"vmath.vector4({res.Groups[1]},{res.Groups[2]}, {res.Groups[3]}, {res.Groups[4]})");
                if (res.Groups[3].Success)
                    return (true, $"vmath.vector4({res.Groups[1]},{res.Groups[2]}, {res.Groups[3]})");
                if (res.Groups[2].Success)
                    return (true, $"vmath.vector4({res.Groups[1]},{res.Groups[2]})");
                if (res.Groups[1].Success)
                    return (true, $"vmath.vector4({res.Groups[1]})");
                return (true, $"vmath.vector4()");
            }
        }

        return (false, node.ToString());
    }


    private static (bool, string) ParseVector3Initializer(SyntaxNode node)
    {
        if (node.IsKind(SyntaxKind.ObjectCreationExpression))
        {
            var str = node.ToString();
            Regex r = new Regex(@"new Vector3\((\d+)?(?:,(\d+))?(?:,(\d+))?\)");
            var res = r.Match(str);
            if (res.Success)
            {
                if (res.Groups[3].Success)
                    return (true, $"vmath.vector3({res.Groups[1]},{res.Groups[2]}, {res.Groups[3]})");
                if (res.Groups[2].Success)
                    return (true, $"vmath.vector3({res.Groups[1]},{res.Groups[2]})");
                if (res.Groups[1].Success)
                    return (true, $"vmath.vector3({res.Groups[1]})");
                return (true, $"vmath.vector3()");
            }
        }

        return (false, node.ToString());
    }
    
    private static (bool, string) ParseVector2Initializer(SyntaxNode node)
    {
        if (node.IsKind(SyntaxKind.ObjectCreationExpression))
        {
            var str = node.ToString();
            Regex r = new Regex(@"new Vector2\((\d+)?(?:,(\d+))?\)");
            var res = r.Match(str);
            if (res.Success)
            {
                if (res.Groups[2].Success)
                    return (true, $"vmath.vector3({res.Groups[1]},{res.Groups[2]})");
                if (res.Groups[1].Success)
                    return (true, $"vmath.vector3({res.Groups[1]})");
                return (true, $"vmath.vector3()");
            }
        }

        return (false, node.ToString());
    }


    private static (bool, string) ParseUrlInitializer(SyntaxNode node)
    {
        if (node.IsKind(SyntaxKind.StringLiteralExpression))
            return (true, $"msg.url({node.ToString()})");

        if (node.IsKind(SyntaxKind.ObjectCreationExpression))
        {
            var str = node.ToString();
            Regex r = new Regex(@"new Url\(""(.*)""\)");
            var res = r.Match(str);
            if (res.Success)
                return (true, $"msg.url({res.Groups[1]})");
        }

        return (false, node.ToString());
    }


    private static (bool, string) ParseHashInitializer(SyntaxNode node)
    {
        if (node.IsKind(SyntaxKind.StringLiteralExpression))
            return (true, $"hash({node.ToString()})");

        if (node.IsKind(SyntaxKind.ObjectCreationExpression))
        {
            var str = node.ToString();
            Regex r = new Regex(@"new Hash\(""(.*)""\)");
            var res = r.Match(str);
            if (res.Success)
                return (true, $"hash({res.Groups[1]})");
        }

        return (false, node.ToString());
    }


    private static void ConstructPropertyFromAttribute(StreamWriter writer, AttributeData DefoldProperty,
        IFieldSymbol fieldSymbol)
    {
        var firstParam = DefoldProperty.ConstructorArguments[0].Value;
        writer.WriteLine(@$"go.property(""{fieldSymbol.Name}"", {firstParam})");
        return;
    }


    public IEnumerable<ITypeSymbol> GetBaseTypes(ITypeSymbol type) {
        var current = type;
        while (current != null) {
            yield return current;
            current = current.BaseType;
        }
    }


    private bool IsFlaggedForScript(INamedTypeSymbol type, out string scriptExtension) {
        var attr = type.GetAttributes().FirstOrDefault(x => x.AttributeClass.ToString().EndsWith("GenScriptAttribute"));
        if (attr != null) {
            scriptExtension = attr.ConstructorArguments.First().Value.ToString();
            return true;
        }
        
        if (type.BaseType != null)
            return IsFlaggedForScript(type.BaseType, out scriptExtension);

        scriptExtension = "";
        return false;
    }
}
