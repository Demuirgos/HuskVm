using iLang.SyntaxDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iLang.Parsers;
using System.Numerics;
using VirtualMachine.Processor;
using VirtualMachine.iLang.Extras;

namespace iLang.Interpreter
{
    public record Value;
    internal record Nil : Value
    {
        public static Nil Instance { get; } = new Nil();
        public override string ToString() => "nil";
    }
    internal record Decimal(double Value) : Value
    {
        public override string ToString() => Value.ToString();
    }
    internal record Boolean(bool Value) : Value
    {
        public override string ToString() => Value.ToString();
    }



    internal class Context(string namespaceName)
    {
        public string CurrentLibrary { get; set; } = namespaceName;
        public Dictionary<string, Value> Variables { get; set; } = new Dictionary<string, Value>();
    }

    internal class FunctionsContext
    {
        public void AddFunction(FunctionDef function)
        {
            Functions[function.Name.Value] = function;
        }

        public void AddFunctions(CompilationUnit compilationUnit)
        {
            foreach(var function in compilationUnit.Body)
            {
                AddFunction(function);
            }
        }

        public Dictionary<string, FunctionDef> Functions { get; set; } = new Dictionary<string, FunctionDef>();
        
        public FunctionDef this[string key]
        {
            get => Functions[key];
            set => Functions[key] = value;
        }
        
    }

    internal class GlobalContext
    {
        public Dictionary<string, FunctionsContext> Libraries { get; set; } = new Dictionary<string, FunctionsContext>();
        public Dictionary<string, Value> GlobalVariables { get; set; } = new Dictionary<string, Value>();
        public Stack<Context> ContextStack { get; set; } = new Stack<Context>();


    }
    internal class Interpreter
    {
        public static Value Interpret<TTimer>(CompilationUnit compilation, ITimer<TTimer>? timer = null)
        {
            timer?.Start();

            var context = new GlobalContext();

            var mainContext = new FunctionsContext();
            mainContext.AddFunctions(compilation);
            context.Libraries[string.Empty] = mainContext;

            foreach (var library in compilation.inludes)
            {
                var libraryContext = new FunctionsContext();
                libraryContext.AddFunctions(library.Value);
                context.Libraries[library.Key] = libraryContext;
            }

            var result = InterpretFunctionCall(string.Empty, context.Libraries[string.Empty]["Main"], [], context);

            timer?.Stop();
            return result;
        }

        private static Value InterpretFunctionCall(string namespaceName, FunctionDef functionDef, Value[] arguments, GlobalContext context)
        {
            var newContext = new Context(namespaceName);
            context.ContextStack.Push(newContext);

            for (int i = 0; i < arguments.Length; i++)
            {
                newContext.Variables[functionDef.Args.Items[i].Value] = arguments[i];
                
            }

            Value returnValue = functionDef.Body switch {
                Block block => InterpretBlock(block, context),
                Expression expr => InterpretExpression(expr, context),
                _ => throw new Exception("Invalid function body")
            };

            context.ContextStack.Pop();

            return returnValue;
        }

        private static Value InterpretBlock(Block body, GlobalContext context)
        {
            foreach(var statement in body.Items)
            {
                switch(statement)
                {
                    case VarDeclaration varDeclaration:
                        context.ContextStack.Peek().Variables[varDeclaration.Name.Value] = InterpretExpression(varDeclaration.Value, context);
                        break;
                    case ReturnStatement returnStatement:
                        return InterpretExpression(returnStatement.Value, context);
                    case Assignment assignment:
                        context.ContextStack.Peek().Variables[assignment.Name.Value] = InterpretExpression(assignment.Value, context);
                        break;
                    case IfStatement ifStatement:
                        if(InterpretExpression(ifStatement.Condition, context) is Boolean condition1 && condition1.Value is true)
                        {
                            return InterpretBlock(ifStatement.True, context);
                        }
                        else
                        {
                            return InterpretBlock(ifStatement.False, context);
                        }
                        break;
                    case WhileStatement whileStatement:
                        while(InterpretExpression(whileStatement.Condition, context) is Boolean condition2 && condition2.Value is true)
                        {
                            var blockValue = InterpretBlock(whileStatement.Body, context);
                            if(blockValue is not Nil)
                            {
                                return blockValue;
                            }
                        }
                        break;
                    default:
                        throw new Exception("Invalid statement");
                }
            }

            return Nil.Instance;
        }

        private static Value InterpretExpression(Expression value, GlobalContext context)
        {
            switch (value)
            {
                case Number number:
                    return new Decimal(number.Value);
                case SyntaxDefinitions.Boolean boolean:
                    return new Boolean(boolean.Value);
                case Identifier identifier:
                    return context.ContextStack.Peek().Variables[identifier.Value];
                case BinaryOp binaryOp:
                    return InterpretBinaryOp(binaryOp, context);
                case UnaryOp unaryOp:
                    return InterpretUnary(unaryOp, context);
                case CallExpr callExpr:
                    var args = callExpr.Args.Items.Select(x => InterpretExpression(x, context)).ToArray();

                    var containingNamespace = callExpr.Function.Values.Length switch
                    {
                        1 => context.ContextStack.Peek().CurrentLibrary,
                        2 => callExpr.Function.Values[0],
                        _ => throw new Exception("Invalid function call")
                    };

                    var func = callExpr.Function.Values.Length switch
                    {
                        1 => context.Libraries[containingNamespace][callExpr.Function.Value],
                        2 => context.Libraries[containingNamespace][callExpr.Function.Values[1]],
                        _ => throw new Exception("Invalid function call")
                    };
                    return InterpretFunctionCall(containingNamespace, func, args, context);
                case ParenthesisExpr parentheses:
                    return InterpretExpression(parentheses.Body, context);
                default:
                    throw new Exception($"Invalid expression : {value}");
            }
        }

        private static Value InterpretUnary(UnaryOp unaryOp, GlobalContext context)
        {
            var right = InterpretExpression(unaryOp.Right, context);

            switch (unaryOp.Op.Value)
            {
                case '!' when right is Boolean boolean:
                    return new Boolean(!boolean.Value);
                case '-' when right is Decimal decimalV:
                    return new Decimal(-decimalV.Value);
                case '+' when right is Decimal decimalV:
                    return new Decimal(decimalV.Value);
                default:
                    throw new Exception("Invalid operator");
            }
        }

        private static Value InterpretBinaryOp(BinaryOp binaryOp, GlobalContext context)
        {
            var left = InterpretExpression(binaryOp.Left, context);
            var right = InterpretExpression(binaryOp.Right, context);

            switch (binaryOp.Op.Value)
            {
                case '+' when left is Decimal leftV && right is Decimal rightV:
                    return new Decimal(leftV.Value + rightV.Value);
                case '-' when left is Decimal leftV && right is Decimal rightV:
                    return new Decimal(leftV.Value - rightV.Value);
                case '*' when left is Decimal leftV && right is Decimal rightV:
                    return new Decimal(leftV.Value * rightV.Value);
                case '/' when left is Decimal leftV && right is Decimal rightV: 
                    return new Decimal(leftV.Value / rightV.Value);
                case '%' when left is Decimal leftV && right is Decimal rightV:
                    return new Decimal(leftV.Value % rightV.Value);
                case '>' when left is Decimal leftV && right is Decimal rightV:
                    return new Boolean(leftV.Value > rightV.Value);
                case '<' when left is Decimal leftV && right is Decimal rightV:
                    return new Boolean(leftV.Value < rightV.Value);
                case '=' :
                    return new Boolean(left.Equals(right));
                case '&' when left is Boolean leftV && right is Boolean rightV:
                    return new Boolean(leftV.Value && rightV.Value);
                case '|' when left is Boolean leftV && right is Boolean rightV:
                    return new Boolean(leftV.Value || rightV.Value);
                case '^' when left is Boolean leftV && right is Boolean rightV:
                    return new Boolean(leftV.Value ^ rightV.Value);
                default:
                    throw new Exception("Invalid operator");
            }
        }
    }
}
