using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLang.SyntaxDefinitions
{
    public record SyntaxTree;

    public record Statement : SyntaxTree;
    public record Expression : SyntaxTree;
    public record Atom : Expression;
    public record Identifier(string Value) : Atom;
    public record Number(double Value) : Atom;
    public record Boolean(bool Value) : Atom;
    public record Operation(char op);
    public record ArgumentList(Identifier[] Items) : SyntaxTree
    {
        public override string ToString() => $"[{string.Join(", ", Items.Select(x => x.ToString()))}]";

    }
    public record FunctionDef(Identifier Name, ArgumentList Args, SyntaxTree Body) : SyntaxTree;
    public record ReturnStatement(Expression Value) : Statement;
    public record BinaryOp(Expression Left, Operation Op, Expression Right) : Expression;
    public record UnaryOp(Operation Op, Expression Right) : Expression;
    public record IfStatement(Expression Condition, Block True, Block False) : Statement;
    public record WhileStatement(Expression Condition, Block Body) : Statement;
    public record ParenthesisExpr(Expression Body) : Expression;
    public record ParameterList(Expression[] Items) : SyntaxTree
    {
        public override string ToString() => $"({string.Join(", ", Items.Select(x => x.ToString()))})";
    }
    public record CallExpr(SyntaxTree Function, ParameterList Args) : Expression;
    public record Block(Statement[] Items) : SyntaxTree
    {
        public override string ToString() => string.Join("\n", Items.Select(x => x.ToString()));
    }
    public record VarDeclaration(Identifier Name, Expression Value) : Statement;
    public record Assignment(Identifier Name, Expression Value) : Statement;
    public record CompilationUnit(SyntaxTree[] Body) : SyntaxTree
    {
        public override string ToString() => string.Join("\n", Body.Select(x => x.ToString()));
    }

}
