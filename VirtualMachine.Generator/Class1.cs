using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace VirtualMachine.Generator {
    [Generator]
    public class BytecodeBuilderGenerator : ISourceGenerator
    {
        private IEnumerable<ClassDeclarationSyntax> GetAllMarkedClasses(Compilation context, string attributeName)
            => context.SyntaxTrees
                .SelectMany(st => st.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => GetMark(c, attributeName) is not null))
                .Where(c => c.BaseList?.Types.Any(t => t.ToString().StartsWith("Instruction")) ?? false);
        private AttributeSyntax GetMark(ClassDeclarationSyntax classDeclaration, string attributeName)
        => classDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(a => a.Name.GetText().ToString().StartsWith(attributeName.Replace("Attribute", String.Empty)))
            .FirstOrDefault();

        public static string GetNamespace(SyntaxNode node)
        {
            if (node.Parent is FileScopedNamespaceDeclarationSyntax or NamespaceDeclarationSyntax)
                return node.Parent switch
                {
                    FileScopedNamespaceDeclarationSyntax f => f.Name.ToString(),
                    NamespaceDeclarationSyntax n => n.Name.ToString(),
                    _ => throw new NotImplementedException(),
                };

            else
                return node.Parent is not null ? GetNamespace(node.Parent) : String.Empty;
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var types = GetAllMarkedClasses(context.Compilation, "Metadata");

            var roots = context.Compilation.SyntaxTrees
                .Select(st => st.GetRoot())
                .Where(st => st.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Any(c => types.Contains(c)));

            // get all using directives from the syntax trees
            var usings = roots
                .SelectMany(r => r.DescendantNodes()
                                  .OfType<UsingDirectiveSyntax>())
                .Distinct();

            // get all namespaces from the syntax trees
            var namespaces = roots
                .SelectMany(r => r.DescendantNodes().OfType<NamespaceDeclarationSyntax>().Select(ns => ns.Name))
                .Concat(roots.SelectMany(r => r.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().Select(fns => fns.Name)))
                .Distinct();

            var builder = new StringBuilder();
            builder.Append("using System;\n");
            builder.Append("using System.Collections.Generic;\n");
            builder.Append("using System.Linq;\n");
            builder.Append("using System.Text;\n");
            foreach (var u in usings)
            {
                builder.Append(u.ToString());
                builder.Append("\n");
            }

            foreach (var @namespace in namespaces)
            {
                builder.Append("using ");
                builder.Append(@namespace);
                builder.Append(";\n");
            }

            builder.Append("namespace VirtualMachine.Builder {\n");
            builder.Append("public static class BytecodeBuilderExt {\n");
            foreach (var type in types)
            {
                string baseClassToken = 
                    type.BaseList.Types
                        .First(t => t.ToString().StartsWith("Instruction"))
                        .ToString();

                string baseClass = baseClassToken.Substring(baseClassToken.IndexOf("<") + 1, baseClassToken.LastIndexOf(">") - baseClassToken.IndexOf("<") - 1);

                var name = type.Identifier.Text;
                var metadata = GetMark(type, "Metadata");
                var arguments = metadata.ArgumentList.Arguments.Select(a => a.ToString());
                var immediateSizes = arguments.Skip(2).Select(a => byte.Parse(a)).ToArray();

                var opcode = type.DescendantNodes().OfType<PropertyDeclarationSyntax>().First(p => p.Identifier.Text == "OpCode").Initializer.Value.ToString();


                var argumentString = immediateSizes.Length == 0 ? String.Empty : "," + string.Join(", ", immediateSizes.Select((size, i) =>
                {
                    string type = size switch
                    {
                        1 => "byte",
                        2 => "ushort",
                        4 => "uint",
                        8 => "ulong",
                        _ => throw new Exception("Invalid immediate size")
                    };
                    return $"{type} immediate{i}";
                }));

                builder.Append($"public static AssemblyBuilder<{baseClass}> {name}(this AssemblyBuilder<{baseClass}> current{argumentString}) {{ \n");
                builder.Append($@"
    current.Bytecode.Add({opcode});
    {string.Join("\n", immediateSizes.Select((size, i) =>
    {
        if(size == 1) return $"current.Bytecode.Add(immediate{i});";
        else return $"current.Bytecode.AddRange(BitConverter.GetBytes(immediate{i}));";
    }))}
    return current;
");

                builder.Append("}\n");
            }
            builder.Append("}\n");
            builder.Append("}\n");
            context.AddSource("BytecodeBuilder.g.cs", builder.ToString());
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }
}
