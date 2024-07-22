using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace VirtualMachine.Generator
{
    internal static class Utils
    {
        public static IEnumerable<ClassDeclarationSyntax> GetAllMarkedClasses(Compilation context, string attributeName)
            => context.SyntaxTrees
                .SelectMany(st => st.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => GetMark(c, attributeName) is not null))
                .Where(c => c.BaseList?.Types.Any(t => t.ToString().StartsWith("Instruction")) ?? false);
        public static AttributeSyntax GetMark(ClassDeclarationSyntax classDeclaration, string attributeName)
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
    }
}
