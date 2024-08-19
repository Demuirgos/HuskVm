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
using System.Xml.Linq;

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
        public override string ToString() => (this.Value ? 1 : 0).ToString();
    }
    internal record Record(Dictionary<string, Value> Fields) : Value
    {
        public override string ToString() => $"{{{string.Join(", ", Fields.Select(x => $"{x.Key}: {x.Value}"))}}}";
    }

    internal record Array(Value[] Items) : Value
    {
        public override string ToString() => $"[{string.Join(", ", Items.Select(i => i.ToString()))}]";
    }


    internal class Context(string namespaceName)
    {
        public string CurrentLibrary { get; set; } = namespaceName;
        public Dictionary<string, Value> Variables { get; set; } = new Dictionary<string, Value>();
    }

    internal class FunctionsContext
    {
        public void AddFunction(FunctionDefinition function)
        {
            Functions[function.Name.FullName] = function;
        }

        public void AddFunctions(CompilationUnit compilationUnit)
        {
            foreach(var function in compilationUnit.Body.Where(f => f is FunctionDefinition))
            {
                AddFunction(function as FunctionDefinition);
            }
        }

        public Dictionary<string, FunctionDefinition> Functions { get; set; } = new Dictionary<string, FunctionDefinition>();
        
        public FunctionDefinition this[string key]
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

            foreach (var library in compilation.FuncInludes)
            {
                var libraryContext = new FunctionsContext();
                libraryContext.AddFunctions(library.Value);
                context.Libraries[library.Key] = libraryContext;
            }

            var result = InterpretFunctionCall(string.Empty, context.Libraries[string.Empty]["Main"], [], context);

            timer?.Stop();
            return result;
        }

        private static Value InterpretFunctionCall(string namespaceName, FunctionDefinition functionDef, Value[] arguments, GlobalContext context)
        {
            var newContext = new Context(namespaceName);
            context.ContextStack.Push(newContext);

            for (int i = 0; i < arguments.Length; i++)
            {
                newContext.Variables[functionDef.Args.Items[i].Name.FullName] = arguments[i];
                
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
                switch (statement)
                {
                    case VarDeclaration varDeclaration:
                        if (varDeclaration.IsGlobal)
                        {
                            context.GlobalVariables[varDeclaration.Name.FullName] = InterpretExpression(varDeclaration.Value, context);
                        }
                        else
                        {
                            context.ContextStack.Peek().Variables[varDeclaration.Name.FullName] = InterpretExpression(varDeclaration.Value, context);
                        }
                        break;
                    case ReturnStatement returnStatement:
                        return InterpretExpression(returnStatement.Value, context);
                    case Assignment assignment when assignment.Name is Name:
                        var target1 = assignment.Name as Name;
                        if (context.ContextStack.Peek().Variables.ContainsKey(target1.FullName))
                        {
                            context.ContextStack.Peek().Variables[target1.FullName] = InterpretExpression(assignment.Value, context);
                        } else if (context.GlobalVariables.ContainsKey(target1.FullName))
                        {
                            context.GlobalVariables[target1.FullName] = InterpretExpression(assignment.Value, context);
                        }
                        else
                        {
                            throw new Exception("Variable not found");
                        }
                        break;
                    case Assignment assignment when assignment.Name is Composed:
                        var target2 = assignment.Name as Composed;
                        if (context.ContextStack.Peek().Variables.ContainsKey(target2.Root.FullName))
                        {
                            HandleIdentifier(target2, context, InterpretExpression(assignment.Value, context));
                        }
                        else if (context.GlobalVariables.ContainsKey(target2.Root.FullName))
                        {
                            HandleIdentifier(target2, context, InterpretExpression(assignment.Value, context));
                        }
                        else
                        {
                            throw new Exception("Variable not found");
                        }
                        break;
                    case IfStatement ifStatement:
                        if(InterpretExpression(ifStatement.Condition, context) is Boolean condition1 && condition1.Value is true)
                        {
                            var blockValue = InterpretBlock(ifStatement.True, context);
                            if(blockValue is not Nil)
                            {
                                return blockValue;
                            }
                        }
                        else
                        {
                            var blockValue = InterpretBlock(ifStatement.False, context);
                            if (blockValue is not Nil)
                            {
                                return blockValue;
                            }
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
                    return HandleIdentifier(identifier, context);
                case Null nullexpr:
                    return Nil.Instance;
                case BinaryOperation binaryOp:
                    return InterpretBinaryOp(binaryOp, context);
                case UnaryOperation unaryOp:
                    return InterpretUnary(unaryOp, context);
                case CallExpression callExpr:
                    var args = callExpr.Args.Items.Select(x => InterpretExpression(x, context)).ToArray();
                    var containingNamespace = callExpr.Function.Namespace;
                    var func = context.Libraries[containingNamespace][callExpr.Function.Value];
                    return InterpretFunctionCall(containingNamespace, func, args, context);
                case ParenthesisExpr parentheses:
                    return InterpretExpression(parentheses.Body, context);
                case RecordExpression recordExpr:
                    var fields = recordExpr.Fields.ToDictionary(x => x.Key, x => InterpretExpression(x.Value, context));
                    return new Record(fields);
                case ArrayExpression arrayExpr:
                    var items = arrayExpr.Items.Select(x => InterpretExpression(x, context)).ToArray();
                    return new Array(items);
                default:
                    throw new Exception($"Invalid expression : {value}");
            }
        }

        private static Value HandleIdentifier(Identifier identifier, GlobalContext context, Value newValue = null)
        {
            Value ResolveSubIdentifier(Value value, out Value returnedValue, params Identifier[] identifiers)
            {
                if (identifiers.Length == 0)
                {
                    returnedValue = value;
                    return value;
                }
                switch (identifiers[0])
                {
                    case Name name:
                        if (value is Record record1 && record1.Fields.ContainsKey(name.FullName))
                        {
                            if(newValue is not null && identifiers.Length == 1)
                            {
                                record1.Fields[name.FullName] = newValue;
                            }
                            returnedValue = ResolveSubIdentifier(record1.Fields[name.FullName], out returnedValue, identifiers.Skip(1).ToArray());
                            return record1;
                        }
                        throw new Exception("Invalid identifier");
                    case Indexer indexer:
                        if (InterpretExpression(indexer.Index, context) is Decimal index && index.Value % 1 == 0)
                        {
                            if (value is Array array)
                            {
                                if (index.Value >= 0 && index.Value < array.Items.Length)
                                {
                                    if (newValue is not null && identifiers.Length == 1)
                                    {
                                        array.Items[(int)index.Value] = newValue;
                                    }
                                    returnedValue = ResolveSubIdentifier(array.Items[(int)index.Value], out returnedValue, identifiers.Skip(1).ToArray());
                                    return array;
                                }
                                throw new Exception("Invalid index");
                            }
                        }
                        throw new Exception("Invalid index");
                    default:
                        throw new Exception("Invalid identifier");
                }
            }
            switch (identifier)
            {
                case Name name:
                    var root1 = name.FullName;
                    if (context.GlobalVariables.ContainsKey(root1))
                    {
                        if(newValue is not null)
                        {
                            context.GlobalVariables[root1] = newValue;
                        }
                        return ResolveSubIdentifier(context.GlobalVariables[root1], out _, []);
                    }
                    else if (context.ContextStack.Peek().Variables.ContainsKey(root1))
                    {
                        if (newValue is not null)
                        {
                            context.ContextStack.Peek().Variables[root1] = newValue;
                        }
                        return ResolveSubIdentifier(context.ContextStack.Peek().Variables[root1], out _, []);
                    }
                    else throw new Exception("Variable not found");
                case Composed composed:
                    var root2 = composed.Root.FullName;
                    if (context.GlobalVariables.ContainsKey(root2))
                    {
                        _ = ResolveSubIdentifier(context.GlobalVariables[root2], out Value value, composed.Values.Skip(1).ToArray());
                        return value;
                    }
                    else if (context.ContextStack.Peek().Variables.ContainsKey(root2))
                    {
                        _ = ResolveSubIdentifier(context.ContextStack.Peek().Variables[root2], out Value value,  composed.Values.Skip(1).ToArray());
                        return value;
                    }
                    else throw new Exception("Variable not found");
                default:
                    throw new Exception("Invalid identifier");
            }
        }

        private static Value InterpretUnary(UnaryOperation unaryOp, GlobalContext context)
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

        private static Value InterpretBinaryOp(BinaryOperation binaryOp, GlobalContext context)
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
