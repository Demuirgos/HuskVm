using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualMachine.iLang.Compilers;

namespace iLang.SyntaxDefinitions
{
    public record SyntaxTree;

    public record Statement : SyntaxTree;
    public record Expression : SyntaxTree;
    public record Atom : Expression;
    public record Identifier(string @namespace, params string[] Values) : Atom
    {
        public string LocalName => string.Join(".", Values);
        public string FullName
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if(!string.IsNullOrEmpty(@namespace))
                {
                    sb.Append(@namespace);
                    sb.Append("::");
                }
                sb.Append(LocalName);
                return sb.ToString();
            }
        }
    }
    public record Number(double Value) : Atom;
    public record String(string Value) : Atom;
    public record Boolean(bool Value) : Atom;
    public record Char(char Value) : Atom;
    public record Operation(char Value);
    public class Type(string Name)
    {
        public string Name { get; init; } = Name;
        public static Type Void = new Type(string.Empty);
        public static Type Any = new Type("Any");
        public static Type Char = new Type("Char");
        public static Type Number = new Type("Number");
        public static Type String = new Type("Word");
        public static Type Bool = new Type("Logic");

        public static implicit operator Type(string value) => new Type(value);

        public static bool operator ==(Type a, Type b) => a.Name.ToLower() == b.Name.ToLower() || (a.Name == Any.Name || b.Name == Any.Name);
        public static bool operator !=(Type a, Type b) => !(a == b);
    }

    public record FieldDef(string Name, Type Type);
    public record TypeDef(string Name, FieldDef[] Fields);

    public record Argument(Identifier Name, Type Type) : SyntaxTree;
    public record ArgumentList(Argument[] Items) : SyntaxTree
    {
        public static ArgumentList Empty = new ArgumentList(new Argument[0]);
        public override string ToString() => $"[{string.Join(", ", Items.Select(x => x.ToString()))}]";

    }
    public record FunctionDef(Identifier Name, Type ReturnType, ArgumentList Args, SyntaxTree Body) : SyntaxTree;
    public record ReturnStatement(Expression Value) : Statement;
    public record BinaryOp(Expression Left, Operation Op, Expression Right) : Expression;
    public record UnaryOp(Operation Op, Expression Right) : Expression;
    public record Indexer(Identifier Name, Expression Index) : Expression;
    public record IfStatement(Expression Condition, Block True, Block False) : Statement;
    public record WhileStatement(Expression Condition, Block Body) : Statement;
    public record ParenthesisExpr(Expression Body) : Expression;
    public record FilePath(string Path, string Alias);
    public record IncludeFile(FilePath[] Paths) : Statement;
    public record ParameterList(Expression[] Items) : SyntaxTree
    {
        public override string ToString() => $"({string.Join(", ", Items.Select(x => x.ToString()))})";
    }
    public record CallExpr(Identifier Function, ParameterList Args) : Expression;
    public record Block(Statement[] Items) : SyntaxTree
    {
        public override string ToString() => string.Join("\n", Items.Select(x => x.ToString()));
    }
    public record VarDeclaration(Identifier Name, Type Type, Expression Value) : Statement;
    public record Assignment(Identifier Name, Expression Value) : Statement;
    public record CompilationUnit(Dictionary<string, CompilationUnit> inludes, FunctionDef[] Body) : SyntaxTree
    {
        public override string ToString() {
            var sb = new StringBuilder();

            if (inludes.Count > 0) {
                sb.AppendLine("Includes:");
                foreach (var item in inludes)
                {
                    sb.AppendLine($"{item.Key}:");
                    sb.AppendLine(item.Value.ToString());
                }
            }

            sb.AppendLine($"toplevel:");
            foreach (var item in Body)
            {
                sb.AppendLine(item.ToString());
            }
            return sb.ToString();
        }
    }

}
