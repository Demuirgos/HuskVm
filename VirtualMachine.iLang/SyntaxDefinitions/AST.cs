﻿using System;
using System.Collections.Generic;
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
    public record Identifier(params string[] Values) : Atom
    {
        public string Value => string.Join(".", Values);
    }
    public record Number(double Value) : Atom;
    public record String(string Value) : Atom;
    public record Boolean(bool Value) : Atom;
    public record Operation(char Value);
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
    public record VarDeclaration(Identifier Name, Expression Value) : Statement;
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
