using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualMachine.iLang.Compilers;
using static VirtualMachine.iLang.Checker.TypeChecker.Environment;

namespace iLang.SyntaxDefinitions
{
    public record SyntaxTree;

    public record Statement : SyntaxTree;
    public record Expression : SyntaxTree;
    public record Atom : Expression;

    public record Identifier : Atom;
    public record Indexer(Expression Index) : Identifier;
    public record Name(string Namespace, string Value) : Identifier
    {
        public string FullName => Namespace.Length == 0 ? Value  : (Namespace + "::" + Value);
    }
    public record Composed(params Identifier[] Values) : Identifier
    {
        public Name Root
        {
            get
            {
                return Values[0] as Name;
            }
        }
    }
    public record Number(double Value) : Atom;
    public record String(string Value) : Atom;
    public record Boolean(bool Value) : Atom;
    public record Char(char Value) : Atom;
    public record Null(string Value) : Atom;
    public record Operation(char Value);

    public class TypeNode(Name name)
    {
        public Name Id { get; init; } = name;

        public override bool Equals(object obj)
        {
            return obj is TypeNode node &&
                   Id.FullName == node.Id.FullName;
        }

        public override int GetHashCode()
        {
            return Id.FullName.GetHashCode();
        }
    }

    public class GenericTypeNode(Name Name, params TypeNode[] Arguments) : TypeNode(new Name(Name.Namespace, $"{Name.FullName}{string.Join("'", Arguments.Select(t => t.Id.FullName))}"))
    {
        public virtual string Name { get; init; } = Name.FullName;
        public TypeNode[] Arguments { get; init; } = Arguments;

    }

    public class ArrayTypeNode(TypeNode InnerType, Number size) : GenericTypeNode(new Name(string.Empty, $"Array'{size.Value}"), InnerType)
    {
        public override string Name { get; init; } = $"{InnerType.Id}[{size.Value}]";
        public TypeNode InnerType { get; init; } = InnerType;
        public Number Size { get; init; } = size;
        public bool IsArray { get; init; } = true;
    }
    
    public class SimpleTypeNode(Name Name) : TypeNode(Name)
    {
        public string Name { get; init; } = Name.FullName;

        public static implicit operator SimpleTypeNode(Name value) => new SimpleTypeNode(value);
    }

    public record FieldDefinition(string Name, TypeNode Type);
    public record TypeDefinition(string Name, FieldDefinition[] Fields) : SyntaxTree;


    public record Argument(Name Name, TypeNode Type) : SyntaxTree;
    public record ArgumentList(Argument[] Items) : SyntaxTree
    {
        public static ArgumentList Empty = new ArgumentList(new Argument[0]);
        public override string ToString() => $"[{string.Join(", ", Items.Select(x => x.ToString()))}]";

    }
    public record FunctionDefinition(Name Name, TypeNode ReturnType, ArgumentList Args, SyntaxTree Body) : SyntaxTree;
    public record ReturnStatement(Expression Value) : Statement;
    public record BinaryOperation(Expression Left, Operation Op, Expression Right) : Expression;
    public record UnaryOperation(Operation Op, Expression Right) : Expression;
    public record IfStatement(Expression Condition, Block True, Block False) : Statement;
    public record WhileStatement(Expression Condition, Block Body) : Statement;
    public record ParenthesisExpr(Expression Body) : Expression;
    public record RecordExpression(TypeNode Type, Dictionary<string, Expression> Fields) : Expression;
    public record ArrayExpression(TypeNode Type, Expression[] Items) : Expression;
    public record FilePath(string Path, string Alias);
    public record IncludeFile(FilePath[] Paths) : Statement;
    public record ParameterList(Expression[] Items) : SyntaxTree
    {
        public override string ToString() => $"({string.Join(", ", Items.Select(x => x.ToString()))})";
    }
    public record CallExpression(Name Function, ParameterList Args) : Expression;
    public record Block(Statement[] Items) : SyntaxTree
    {
        public override string ToString() => string.Join("\n", Items.Select(x => x.ToString()));
    }
    public record VarDeclaration(Name Name, TypeNode Type, bool IsGlobal, Expression Value) : Statement;
    public record Assignment(Identifier Name, Expression Value) : Statement;
    public record CompilationUnit(Dictionary<string, CompilationUnit> TypeInludes, Dictionary<string, CompilationUnit> FuncInludes, SyntaxTree[] Body) : SyntaxTree
    {
        public override string ToString() {
            var sb = new StringBuilder();

            if (FuncInludes.Count > 0) {
                sb.AppendLine("Includes:");
                foreach (var item in FuncInludes)
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
