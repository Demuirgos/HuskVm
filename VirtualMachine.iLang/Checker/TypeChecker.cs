using iLang.SyntaxDefinitions;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using static VirtualMachine.iLang.Checker.TypeChecker.Environment;
using Boolean = iLang.SyntaxDefinitions.Boolean;
using Char = iLang.SyntaxDefinitions.Char;
using String = iLang.SyntaxDefinitions.String;
using TypeDefinition = iLang.SyntaxDefinitions.TypeDefinition;
using TypeNode = iLang.SyntaxDefinitions.TypeNode;

namespace VirtualMachine.iLang.Checker
{
    internal static class TypeChecker
    {
        public class Environment
        {
            internal class TypeDef
            {
                public static bool operator ==(TypeDef a, TypeDef b) => a.Equals(b);
                public static bool operator !=(TypeDef a, TypeDef b) => !a.Equals(b);

                public override bool Equals(object obj)
                {
                    if(obj.GetHashCode() == PrimitiveType.Null.GetHashCode()) return true;
                    if(this.GetHashCode() == PrimitiveType.Null.GetHashCode()) return true;

                    if(obj.GetType() != GetType())
                    {
                        return false;
                    }   

                    switch (this)
                    {
                        case PrimitiveType p:
                            return p.GetHashCode() == ((PrimitiveType)obj).GetHashCode();
                        case RecordType r:
                            return r.GetHashCode() == ((RecordType)obj).GetHashCode();
                        case FunctionType f:
                            return f.GetHashCode() == ((FunctionType)obj).GetHashCode();
                        case ArrayType a:
                            return a.GetHashCode() == ((ArrayType)obj).GetHashCode();
                        case GenericType g:
                            return g.GetHashCode() == ((GenericType)obj).GetHashCode();
                        default:
                            return false;
                    }
                }

                public override int GetHashCode()
                {
                    switch (this)
                    {
                        case PrimitiveType p:
                            return p.Name.GetHashCode();
                        case RecordType r:
                            return r.Fields.Select(f => f.Value.GetHashCode()).Sum().GetHashCode();
                        case FunctionType f:
                            return f.Args.Select(a => a.GetHashCode()).Sum().GetHashCode() ^ f.ReturnType.GetHashCode();
                        case ArrayType a:
                            return a.InnerType.GetHashCode() ^ a.Size.GetHashCode();
                        case GenericType g:
                            return g.Id.GetHashCode() ^ g.Arguments.Select(g => g.GetHashCode()).Sum().GetHashCode();
                        default:
                            return 0;
                    }
                }
            }
            internal class PrimitiveType(SimpleTypeNode name) : TypeDef
            {
                public TypeNode Name { get; init; } = name as TypeNode; 

                public static implicit operator PrimitiveType(SimpleTypeNode type) => new(type);
                public static PrimitiveType Null => new PrimitiveType(new SimpleTypeNode(new Name(string.Empty, "Null")));
                public static PrimitiveType Number => new PrimitiveType(new SimpleTypeNode(new Name(string.Empty, "Number")));
                public static PrimitiveType Char => new PrimitiveType(new SimpleTypeNode(new Name(string.Empty, "Char")));
                public static PrimitiveType Logic => new PrimitiveType(new SimpleTypeNode(new Name(string.Empty, "Logic")));
                public static PrimitiveType Void => new PrimitiveType(new SimpleTypeNode(new Name(string.Empty, "Void")));
            }
            internal class RecordType(Dictionary<string, TypeDef> fields) : TypeDef
            {
                public TypeDef this[string name] => Fields[name];
                public Dictionary<string, TypeDef> Fields { get; init; } = fields;
            }
            internal class FunctionType(TypeDef[] args, TypeDef returnType) : TypeDef
            {
                public TypeDef[] Args { get; init; } = args;
                public TypeDef ReturnType { get; init; } = returnType;
            }
            internal class ArrayType(TypeDef innerType, int size) : TypeDef
            {
                public TypeDef InnerType { get; init; } = innerType;
                public int Size { get; init; } = size;
            }
            internal class GenericType(Name name, TypeDef[] arguments) : TypeDef
            {
                public Name Id { get; init; } = name;
                public TypeDef[] Arguments { get; init; } = arguments;
            }
            private Environment(Environment environment)
            {
                Variables = new(environment.Variables);
            }

            private Environment()
            {
                Variables = new();
            }

            public static Environment From(Environment environment) => new(environment);
            public static Environment Empty => new Environment();
            public Dictionary<string, TypeDef> Variables { get; init; } 
            public Dictionary<string, FunctionType> Functions { get; init; }  = new();
            public Dictionary<int, TypeDef> TypeDefinitions { get; init; } = new Dictionary<int, TypeDef>{
                [PrimitiveType.Number.Name.Id.GetHashCode()] = (PrimitiveType)Environment.PrimitiveType.Number,
                [PrimitiveType.Char.Name.Id.GetHashCode()] = (PrimitiveType)Environment.PrimitiveType.Char,
                [PrimitiveType.Logic.Name.Id.GetHashCode()] = (PrimitiveType)Environment.PrimitiveType.Logic,
                [PrimitiveType.Void.Name.Id.GetHashCode()] = (PrimitiveType)Environment.PrimitiveType.Void
            };

            public void AddType(TypeNode type, TypeDef typeDef)
            {
                int key = type.Id.GetHashCode();
                TypeDefinitions[key] = typeDef;
            }

            public bool VerifyType(TypeNode type, out TypeDef typedef)
            {
                if(type is null)
                {
                    typedef = PrimitiveType.Null;
                    return true;
                }
                int key = type.Id.GetHashCode();
                if(type is ArrayTypeNode array)
                {
                    if (!TypeDefinitions.ContainsKey(key) && VerifyType(array.InnerType, out TypeDef innerType))
                    {
                        TypeDefinitions[key] = new ArrayType(innerType, (int)array.Size.Value);
                    }
                    typedef = TypeDefinitions[key];
                    return true;
                }
                else if(type is GenericTypeNode genType)
                {
                    if(!TypeDefinitions.ContainsKey(key))
                    {
                        var args = genType.Arguments.Select(x => VerifyType(x, out TypeDef typeArg) ? typeArg : throw new Exception("Invalid Type")).ToArray();
                        TypeDefinitions[key] = new GenericType(genType.Id, args);
                    }
                    typedef = TypeDefinitions[key];
                    return true;
                } 
                else if(TypeDefinitions.ContainsKey(key))
                {
                    typedef = TypeDefinitions[key];
                    return true;
                }
                typedef = PrimitiveType.Null;
                return false;
            }

            public Dictionary<string, Dictionary<string, FunctionType>> FunctionLibraries { get; init; } = new();
        }

        public class Context
        {
            public Environment Globals { get; init; }
            public string CurrentFunction { get; set; }
            public Dictionary<string, Environment.TypeDef> Variables { get; init; } = new();
        }

        private static bool TypeOf(Atom atom, Context context, Environment env, out Environment.TypeDef type)
        {
            Environment.TypeDef ResolveName(Identifier name)
            {
                Environment.TypeDef TraverseType(Environment.TypeDef type, Identifier[] identifiers)
                {
                    if (identifiers.Length == 0) return type;

                    var name = identifiers[0];
                    if(name is Name n && type is Environment.RecordType record && record.Fields.ContainsKey(n.Value))
                    {
                        return TraverseType(record.Fields[n.Value], identifiers.Skip(1).ToArray());
                    } else if (name is Indexer indexer && type is Environment.ArrayType array)
                    {
                        return TraverseType(array.InnerType, identifiers.Skip(1).ToArray());
                    }

                    throw new Exception("Invalid type");
                }

                switch(name)
                {
                    case Name n:
                        if(context.Variables.ContainsKey(n.FullName))
                        {
                            return context.Variables[n.FullName];
                        }
                        if (env.Variables.ContainsKey(n.FullName))
                        {
                            return env.Variables[n.FullName];
                        }
                        break;
                    case Composed c:
                        var root = c.Root as Name;
                        if(context.Variables.ContainsKey(root.FullName))
                        {
                            return TraverseType(context.Variables[root.FullName], c.Values.Skip(1).ToArray());
                        }
                        if (env.Variables.ContainsKey(root.FullName))
                        {
                            return TraverseType(env.Variables[root.FullName], c.Values.Skip(1).ToArray());
                        }
                        break;
                }

                return Environment.PrimitiveType.Null;
            }

            type = atom switch
            {
                Identifier id => ResolveName(id),
                Number _ => Environment.PrimitiveType.Number,
                Char _ => Environment.PrimitiveType.Char,
                Boolean _ => Environment.PrimitiveType.Logic,
                _ => Environment.PrimitiveType.Null
            };
            return true;
        }

        private static bool TypeOf(BinaryOperation binaryOp, Context context, Environment env, out Environment.TypeDef type)
        {
            if (!TypeOf(binaryOp.Left, context, env, out var left) || !TypeOf(binaryOp.Right, context, env, out var right))
            {
                type = Environment.PrimitiveType.Null;
                return false;
            }

            var (requiredType, resultType) = binaryOp.Op.Value switch
            {
                '+' => (Environment.PrimitiveType.Number, Environment.PrimitiveType.Number),
                '-' => (Environment.PrimitiveType.Number, Environment.PrimitiveType.Number),
                '*' => (Environment.PrimitiveType.Number, Environment.PrimitiveType.Number),
                '/' => (Environment.PrimitiveType.Number, Environment.PrimitiveType.Number),
                '%' => (Environment.PrimitiveType.Number, Environment.PrimitiveType.Number),
                '<' => (Environment.PrimitiveType.Number, Environment.PrimitiveType.Logic),
                '>' => (Environment.PrimitiveType.Number, Environment.PrimitiveType.Logic),
                '&' => (Environment.PrimitiveType.Number, Environment.PrimitiveType.Logic),
                '|' => (Environment.PrimitiveType.Logic, Environment.PrimitiveType.Logic),
                '^' => (Environment.PrimitiveType.Logic, Environment.PrimitiveType.Logic),
                '=' => (Environment.PrimitiveType.Null, Environment.PrimitiveType.Logic),
                _ => (Environment.PrimitiveType.Null, Environment.PrimitiveType.Null)
            };

            if (left != right && requiredType != left)
            {
                type = Environment.PrimitiveType.Null;
                return false;
            }

            type = resultType;
            return true;
        }

        private static bool TypeOf(UnaryOperation unaryOp, Context context, Environment env, out Environment.TypeDef type)
        {
            if (!TypeOf(unaryOp.Right, context, env, out var right))
            {
                type = Environment.PrimitiveType.Null;
                return false;
            }

            var (requiredType, resultType) = unaryOp.Op.Value switch
            {
                '+' => (Environment.PrimitiveType.Number, Environment.PrimitiveType.Number),
                '-' => (Environment.PrimitiveType.Number, Environment.PrimitiveType.Number),
                '!' => (Environment.PrimitiveType.Logic, Environment.PrimitiveType.Logic),
                _ => (Environment.PrimitiveType.Null, Environment.PrimitiveType.Null)
            };

            if (requiredType != right)
            {
                type = Environment.PrimitiveType.Null;
                return false;
            }

            type = resultType;
            return true;
        }

        private static bool TypeOf(CallExpression callExpr, Context context, Environment env, out Environment.TypeDef type)
        {
            bool isLocalFunction = string.IsNullOrEmpty(callExpr.Function.Namespace);

            var expectedArgs = isLocalFunction 
                ? context.Globals.Functions[callExpr.Function.FullName] 
                : context.Globals.FunctionLibraries[callExpr.Function.Namespace][callExpr.Function.Value];

            var args = callExpr.Args.Items;

            if (args.Length != expectedArgs.Args.Length)
            {
                type = Environment.PrimitiveType.Null;
                return false;
            }

            for (var i = 0; i < args.Length; i++)
            {
                if (!TypeOf(args[i], context, env, out var argType) || argType != expectedArgs.Args[i])
                {
                    type = Environment.PrimitiveType.Null;
                    return false;
                }
            }

            type = expectedArgs.ReturnType;
            return true;
        }


        private static bool TypeOf(ParenthesisExpr parenthesisExpr, Context context, Environment env, out Environment.TypeDef type)
        {
            return TypeOf(parenthesisExpr.Body, context, env, out type);
        }

        private static bool TypeOf(ArrayExpression arrayExpr, Context context, Environment env, out Environment.TypeDef type)
        {
            env.VerifyType(arrayExpr.Type, out var declaredType);
            type = Environment.PrimitiveType.Null;

            var bodyTypes = arrayExpr.Items.Select(item => TypeOf(item, context, env, out Environment.TypeDef itemType) ? itemType : null).Where(type => type is not null).ToList();
            if (bodyTypes.Count != arrayExpr.Items.Length) return false;

            var bodyType = bodyTypes.ToHashSet();
            if (bodyType.Count() > 1) 
                return false;
            if (bodyTypes.Count > 0 && bodyTypes[0] != declaredType) 
                return false;

            type = new Environment.ArrayType(declaredType, size: arrayExpr.Items.Length);
            return true;
        }

        private static bool TypeOf(RecordExpression recordExpr, Context context, Environment env, out Environment.TypeDef type)
        {
            var fields = recordExpr.Fields;

            if(!env.VerifyType(recordExpr.Type, out var declaredType))
            {
                type = Environment.PrimitiveType.Null;
                return false;
            }

            env.VerifyType(recordExpr.Type, out var recordType);
            var recordTypeCast = recordType as Environment.RecordType;

            foreach (var (name, expr) in fields)
            {
                if (!TypeOf(expr, context, env, out var fieldType) || fieldType != recordTypeCast[name])
                {
                    type = Environment.PrimitiveType.Null;
                    return false;
                }
            }

            type = recordType;
            return true;
        }

        private static bool TypeOf(Expression expr, Context context, Environment env, out Environment.TypeDef type)
        {
            type = Environment.PrimitiveType.Null;
            return expr switch
            {
                Atom atom => TypeOf(atom, context, env, out type),
                BinaryOperation binary => TypeOf(binary, context, env, out type),
                UnaryOperation unary => TypeOf(unary, context, env, out type),
                CallExpression call => TypeOf(call, context, env, out type),
                ParenthesisExpr paren => TypeOf(paren, context, env, out type),
                RecordExpression record => TypeOf(record, context, env, out type),
                ArrayExpression array => TypeOf(array, context, env, out type),
                _ => false
            };
        }

        private static bool Handle(VarDeclaration varDecl, Context context, Environment env)
        {
            var name = varDecl.Name.FullName;
            env.VerifyType(varDecl.Type, out var type);

            if(varDecl.IsGlobal && (context.Globals.Variables.ContainsKey(name) || env.Variables.ContainsKey(name)))
            {
                return false;
            } 
            else if (!varDecl.IsGlobal && context.Variables.ContainsKey(name))
            {
                return false;
            }

            if (TypeOf(varDecl.Value, context, env, out var valueType) && valueType == type)
            {
                if(varDecl.IsGlobal)
                {
                    env.Variables[name] = valueType;
                }
                else
                {
                    context.Variables[name] = valueType;
                }
                return true;
            }

            return false;
        }

        private static bool Handle(Assignment assignment, Context context, Environment env)
        {
            var value = assignment.Value;
            
            var identifierType = TypeOf(assignment.Name, context, env, out var nameType);

            if (TypeOf(value, context, env, out var valueType) && valueType == nameType)
            {
                return true;
            }

            return false;
        }

        private static bool Handle(ReturnStatement returnStatement, Context context, Environment env)
        {
            return TypeOf(returnStatement.Value, context, env, out Environment.TypeDef type) && type == context.Globals.Functions[context.CurrentFunction].ReturnType;
        }

        private static bool Handle(IfStatement ifStatement, Context context, Environment env)
        {
            return TypeOf(ifStatement.Condition, context, env, out var condType) && condType == Environment.PrimitiveType.Logic &&
                   Handle(ifStatement.True, context, env) &&
                   Handle(ifStatement.False, context, env);
        }

        private static bool Handle(WhileStatement whileStatement, Context context, Environment env)
        {
            return  TypeOf(whileStatement.Condition, context, env, out var condType) && condType == Environment.PrimitiveType.Logic &&
                    Handle(whileStatement.Body, context, env);
        }

        private static bool Handle(Block block, Context context, Environment env)
        {
            return block.Items.All(x => Handle(x, context, env));
        }

        private static bool Handle(Statement statement, Context context, Environment env)
        {
            return statement switch
            {
                VarDeclaration varDecl => Handle(varDecl, context, env),
                Assignment assignment => Handle(assignment, context, env),
                ReturnStatement returnStatement => Handle(returnStatement, context, env),
                IfStatement ifStatement => Handle(ifStatement, context, env),
                WhileStatement whileStatement => Handle(whileStatement, context, env),
                _ => false
            };
        }

        private static bool Handle(FunctionDefinition functionDef, Environment context)
        {
            var name = functionDef.Name.FullName;
            context.VerifyType(functionDef.ReturnType, out var returnType);
            var args = functionDef.Args.Items;
            var body = functionDef.Body;

            var argTypes = args.Select(x => (context.VerifyType(x.Type, out var argType), (x.Name, argType)).Item2).ToArray();

            context.Functions[name] = new Environment.FunctionType(argTypes.Select((npt) => npt.argType).ToArray(), returnType);

            var newContext = new Context
            {
                CurrentFunction = name,
                Globals = context,
                Variables = argTypes.ToDictionary(x => x.Name.FullName, x => x.argType)
            };



            return body switch
            {
                Block block => Handle(block, newContext, context),
                Expression expr => TypeOf(expr, newContext, context, out var type) && type == returnType,
            };
        }

        public static bool Handle(string Namespace, TypeDefinition typeDef, Environment context, out Environment.TypeDef typeInstance)
        {
            var name = new SimpleTypeNode(new Name(Namespace, typeDef.Name));
            var fields = typeDef.Fields;
            typeInstance = null;

            if (context.VerifyType(name, out _))
            {
                return false;
            }

            var fieldTypes = fields.ToDictionary(x => x.Name, x => context.VerifyType(x.Type, out var fieldType) ? fieldType : throw new Exception("invalid field type"));
            typeInstance = new Environment.RecordType(fieldTypes);

            context.AddType(name, typeInstance);
            return true;
        }

        public static bool Check(CompilationUnit compilationUnit, Environment preEnv = null)
        {
            var env = preEnv is null? Environment.Empty : Environment.From(preEnv);

            foreach (var function in compilationUnit.Body.Cast<FunctionDefinition>())
            {
                if (function.Name.FullName == "Main") continue;
                var argTypes = function.Args.Items.Select(x => (env.VerifyType(x.Type, out var argType), argType).Item2).ToArray();
                env.VerifyType(function.ReturnType, out var expectedType);
                env.Functions[function.Name.FullName] = new Environment.FunctionType(argTypes, expectedType);
            }

            foreach (var funcLibrary in compilationUnit.FuncInludes)
            {
                if (!env.FunctionLibraries.ContainsKey(funcLibrary.Key))
                {
                    env.FunctionLibraries[funcLibrary.Key] = new();
                }
                foreach (var function in funcLibrary.Value.Body.Cast<FunctionDefinition>())
                {
                    var argTypes = function.Args.Items.Select(x => (env.VerifyType(x.Type, out var argType), argType).Item2).ToArray();
                    env.VerifyType(function.ReturnType, out var expectedType);
                    env.FunctionLibraries[funcLibrary.Key][function.Name.FullName] = new Environment.FunctionType(argTypes, expectedType);
                }
            }

            foreach (var typeLibrary in compilationUnit.TypeInludes)
            {
                foreach (var type in typeLibrary.Value.Body.Cast<TypeDefinition>())
                {
                    Handle(typeLibrary.Key, type, env, out var typeInstance);
                }
            }


            bool isCurrentValid = compilationUnit.Body.All(x => Handle(x as FunctionDefinition, env));
            bool areLibsValid = compilationUnit.FuncInludes.All(x => Check(x.Value, env));
            return isCurrentValid && areLibsValid;
        }
    }
}
