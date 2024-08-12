using iLang.SyntaxDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boolean = iLang.SyntaxDefinitions.Boolean;
using Char = iLang.SyntaxDefinitions.Char;
using String = iLang.SyntaxDefinitions.String;
using Type = iLang.SyntaxDefinitions.Type;

namespace VirtualMachine.iLang.Checker
{
    internal static class TypeChecker
    {
        public class Environment
        {
            public Dictionary<string, (Type[], Type)> Functions { get; init; }  = new();
            public Dictionary<string, Dictionary<string, (Type[], Type)>> Libraries { get; init; } = new();
        }

        public class Context
        {
            public Environment Globals { get; init; }
            public string CurrentFunction { get; set; }
            public Dictionary<string, Type> Variables { get; init; } = new();

        }

        private static bool TypeOf(Atom atom, Context context, out Type type)
        {
            type = atom switch
            {
                Identifier id => context.Variables[id.LocalName],
                Number _ => Type.Number,
                String _ => Type.String,
                Boolean _ => Type.Bool,
                Char _ => Type.Char,
                _ => Type.Any
            };
            return true;
        }

        private static bool TypeOf(BinaryOp binaryOp, Context context, out Type type)
        {
            if (!TypeOf(binaryOp.Left, context, out var left) || !TypeOf(binaryOp.Right, context, out var right))
            {
                type = Type.Any;
                return false;
            }

            var (requiredType, resultType) = binaryOp.Op.Value switch
            {
                '+' => (Type.Number, Type.Number),
                '-' => (Type.Number, Type.Number),
                '*' => (Type.Number, Type.Number),
                '/' => (Type.Number, Type.Number),
                '%' => (Type.Number, Type.Number),
                '<' => (Type.Number, Type.Bool),
                '>' => (Type.Number, Type.Bool),
                '&' => (Type.Number, Type.Bool),
                '|' => (Type.Bool, Type.Bool),
                '^' => (Type.Bool, Type.Bool),
                '=' => (Type.Any, Type.Bool),
                _ => (Type.Any, Type.Any)
            };

            if (left != right && requiredType != left)
            {
                type = Type.Any;
                return false;
            }

            type = resultType;
            return true;
        }

        private static bool TypeOf(UnaryOp unaryOp, Context context, out Type type)
        {
            if (!TypeOf(unaryOp.Right, context, out var right))
            {
                type = Type.Any;
                return false;
            }

            var (requiredType, resultType) = unaryOp.Op.Value switch
            {
                '+' => (Type.Number, Type.Number),
                '-' => (Type.Number, Type.Number),
                '!' => (Type.Bool, Type.Bool),
                _ => (Type.Any, Type.Any)
            };

            if (requiredType != right)
            {
                type = Type.Any;
                return false;
            }

            type = resultType;
            return true;
        }

        private static bool TypeOf(CallExpr callExpr, Context context, out Type type)
        {
            bool isLocalFunction = string.IsNullOrEmpty(callExpr.Function.@namespace);

            var expectedArgs = isLocalFunction 
                ? context.Globals.Functions[callExpr.Function.LocalName] 
                : context.Globals.Libraries[callExpr.Function.@namespace][callExpr.Function.LocalName];

            var args = callExpr.Args.Items;

            if (args.Length != expectedArgs.Item1.Length)
            {
                type = Type.Any;
                return false;
            }

            for (var i = 0; i < args.Length; i++)
            {
                if (!TypeOf(args[i], context, out var argType) || argType != expectedArgs.Item1[i])
                {
                    type = Type.Any;
                    return false;
                }
            }

            type = expectedArgs.Item2;
            return true;
        }

        private static bool TypeOf(Indexer indexer, Context context, out Type type)
        {
            var name = indexer.Name.LocalName;

            if (!TypeOf(indexer.Index, context, out type) || type != Type.Number)
            {
                type = Type.Any;
                return false;
            }

            type = context.Variables[name];
            return true;
        }

        private static bool TypeOf(ParenthesisExpr parenthesisExpr, Context context, out Type type)
        {
            return TypeOf(parenthesisExpr.Body, context, out type);
        }


        private static bool TypeOf(Expression expr, Context context, out Type type)
        {
            type = Type.Any;
            return expr switch
            {
                Atom atom => TypeOf(atom, context, out type),
                BinaryOp binary => TypeOf(binary, context, out type),
                UnaryOp unary => TypeOf(unary, context, out type),
                Indexer indexer => TypeOf(indexer, context, out type),
                CallExpr call => TypeOf(call, context, out type),
                ParenthesisExpr paren => TypeOf(paren, context, out type),
                _ => false
            };
        }

        private static bool Handle(VarDeclaration varDecl, Context context)
        {
            var name = varDecl.Name.LocalName;
            var type = varDecl.Type;

            if (context.Variables.ContainsKey(name))
            {
                return false;
            }

            if (TypeOf(varDecl.Value, context, out var valueType) && valueType == type)
            {
                context.Variables[name] = type;
                return true;
            }

            return false;
        }

        private static bool Handle(Assignment assignment, Context context)
        {
            var name = assignment.Name.LocalName;
            var value = assignment.Value;

            if (!context.Variables.ContainsKey(name))
            {
                return false;
            }

            if (TypeOf(value, context, out var valueType) && valueType == context.Variables[name])
            {
                return true;
            }

            return false;
        }

        private static bool Handle(ReturnStatement returnStatement, Context context)
        {
            return TypeOf(returnStatement.Value, context, out Type type) && type == context.Globals.Functions[context.CurrentFunction].Item2;
        }

        private static bool Handle(IfStatement ifStatement, Context context)
        {
            return TypeOf(ifStatement.Condition, context, out var condType) && condType == Type.Bool &&
                   Handle(ifStatement.True, context) &&
                   Handle(ifStatement.False, context);
        }

        private static bool Handle(WhileStatement whileStatement, Context context)
        {
            return  TypeOf(whileStatement.Condition, context, out var condType) && condType == Type.Bool &&
                    Handle(whileStatement.Body, context);
        }

        private static bool Handle(Block block, Context context)
        {
            return block.Items.All(x => Handle(x, context));
        }

        private static bool Handle(Statement statement, Context context)
        {
            return statement switch
            {
                VarDeclaration varDecl => Handle(varDecl, context),
                Assignment assignment => Handle(assignment, context),
                ReturnStatement returnStatement => Handle(returnStatement, context),
                IfStatement ifStatement => Handle(ifStatement, context),
                WhileStatement whileStatement => Handle(whileStatement, context),
                _ => false
            };
        }

        private static bool Handle(FunctionDef functionDef, Environment context)
        {
            var name = functionDef.Name.LocalName;
            var returnType = functionDef.ReturnType;
            var args = functionDef.Args.Items;
            var body = functionDef.Body;

            var argTypes = args.Select(x => x.Type).ToArray();
            var expectedType = (argTypes, returnType);

            context.Functions[name] = expectedType;

            var newContext = new Context
            {
                CurrentFunction = name,
                Globals = context,
                Variables = args.ToDictionary(x => x.Name.LocalName, x => x.Type)
            };



            return body switch
            {
                Block block => Handle(block, newContext),
                Expression expr => TypeOf(expr, newContext, out var type) && type == returnType,
            };
        }

        public static bool Check(CompilationUnit compilationUnit)
        {
            var env = new Environment();

            foreach (var function in compilationUnit.Body)
            {
                var argType = function.Args.Items.Select(x => x.Type).ToArray();
                var expectedType = function.ReturnType;
                env.Functions[function.Name.LocalName] = (argType, expectedType);
            }

            foreach (var library in compilationUnit.inludes)
            {
                foreach (var function in library.Value.Body)
                {
                    var argType = function.Args.Items.Select(x => x.Type).ToArray();
                    var expectedType = function.ReturnType;
                    if (!env.Libraries.ContainsKey(library.Key))
                    {
                        env.Libraries[library.Key] = new();
                    }
                    env.Libraries[library.Key][function.Name.LocalName] = (argType, expectedType);
                }
            }

            return compilationUnit.Body.All(x => Handle(x, env)) && compilationUnit.inludes.All(x => Check(x.Value));
        }
    }
}
