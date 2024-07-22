using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualMachine.Generator
{
    internal class SingletonBuilder
    {
        public static void EmitCode(GeneratorExecutionContext context)
        {
            var types = Utils.GetAllMarkedClasses(context.Compilation, "Metadata");

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

            builder.Append("namespace VirtualMachine.Instructions.InstructionsExt {\n");
            types.GroupBy(t => t.BaseList?.Types.FirstOrDefault()?.Type.ToString() ?? "object")
                .ToList()
                .ForEach(g =>
                {
                    // base class
                    string baseClass = g.Key.Substring(g.Key.IndexOf("<") + 1, g.Key.LastIndexOf(">") - g.Key.IndexOf("<") - 1);
                    
                    // namespace

                    builder.Append($"public static class {baseClass}Ext {{\n");
                    foreach (var type in g)
                    {
                        var ns = Utils.GetNamespace(type);

                        string name = type.Identifier.Text;
                        string code = @"
                            public static Instruction<{{baseClass}}> {{name}} { get; } = new {{namespace}}.Instructions.{{name}}();
                        ";
                        code = code.Replace("{{name}}", name).Replace("{{baseClass}}", baseClass).Replace("{{namespace}}", ns);
                        builder.Append(code);
                    }
                    builder.Append("}\n");
                });
            builder.Append("}\n");
            context.AddSource("SingletonOpcode.g.cs", builder.ToString());
        }
    }
}
