using iLang.Interpreter;
using iLang.SyntaxDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VirtualMachine.Example.Register.Instructions;
using VirtualMachine.Example.Register;
using Sigil.NonGeneric;
using Boolean = iLang.SyntaxDefinitions.Boolean;
using Sigil;
using static VirtualMachine.Example.Stack.Instructions;
using static VirtualMachine.iLang.Checker.TypeChecker;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Number = iLang.SyntaxDefinitions.Number;
using VirtualMachine.iLang.Compilers;
using CommandLine;

namespace iLang.Compilers
{
    public static class CLRTarget
    {
        internal class LocalEmitionContext
        {
            public Dictionary<string, Local> Locals;
            public Dictionary<string, int> Args;
            public Emit Emitter;
        }
        internal class GlobalEmitionContext
        {
            public string NameSpace;
            public string Current;
            public Dictionary<string, LocalEmitionContext> Methods;
        }

        private static void CompileIdentifier(Identifier identifier, GlobalEmitionContext typeContext)
        {
            LocalEmitionContext context = typeContext.Methods[typeContext.Current];
            if (context.Args.ContainsKey(identifier.FullName))
            {
                context.Emitter.LoadArgument((ushort)context.Args[identifier.FullName]);
            }
            else if (context.Locals.ContainsKey(identifier.FullName))
            {
                context.Emitter.LoadLocal(context.Locals[identifier.FullName]);
            }
            else
            {
                throw new Exception($"Variable {identifier.FullName} not found");
            }
        }

        private static void CompileBoolean(Boolean boolean, GlobalEmitionContext typeContext)
        {
            Emit currentMethod = typeContext.Methods[typeContext.Current].Emitter;

            currentMethod.LoadConstant(boolean.Value);
        }

        private static void CompileNumber(Number number, GlobalEmitionContext typeContext)
        {
            Emit currentMethod = typeContext.Methods[typeContext.Current].Emitter;

            currentMethod.LoadConstant((int)number.Value);
        }

        private static void CompileCall(CallExpr call, GlobalEmitionContext typeContext)
        {
            Emit currentMethod = typeContext.Methods[typeContext.Current].Emitter;
            
            var calledFuncName = Tools.Mangle(typeContext.NameSpace, call.Function);

            // very very very bad workaround
            int argumentMemoryLocation = 0;
            foreach (var arg in call.Args.Items)
            {
                CompileExpression(arg, typeContext);
            }

            currentMethod.Call(typeContext.Methods[calledFuncName].Emitter);
        }

        private static void CompileBinaryOp(BinaryOp binaryOp, GlobalEmitionContext typeContext)
        {
            Emit currentMethod = typeContext.Methods[typeContext.Current].Emitter;

            CompileExpression(binaryOp.Left, typeContext);
            CompileExpression(binaryOp.Right, typeContext);


            switch(binaryOp.Op.Value) 
            {
                case '+': currentMethod.Add(); break;
                case '-': currentMethod.Subtract(); break;
                case '*': currentMethod.Multiply(); break;
                case '/': currentMethod.Divide(); break;
                case '%': currentMethod.Remainder(); break;
                case '<': currentMethod.CompareLessThan(); break;
                case '>': currentMethod.CompareGreaterThan(); break;
                case '=': currentMethod.CompareEqual(); break;
                case '^': currentMethod.Xor(); break;
                case '&': currentMethod.And(); break;
                case '|': currentMethod.Or(); break;
                default: throw new NotImplementedException();
            };
        }

        private static void CompileUnaryOp(UnaryOp unaryOp, GlobalEmitionContext typeContext)
        {
            Emit currentMethod = typeContext.Methods[typeContext.Current].Emitter;

            CompileExpression(unaryOp.Right, typeContext);
            switch (unaryOp.Op.Value)
            {
                case '!':
                    currentMethod.Not();
                    break;
                case '-':
                    currentMethod.Negate();
                    break;
                case '+':
                    // do nothing
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void CompileParenthesis(ParenthesisExpr parenthesis, GlobalEmitionContext typeContext)
        {
            CompileExpression(parenthesis.Body, typeContext);
        }

        private static void CompileExpression(Expression expression, GlobalEmitionContext typeContext)
        {
            switch (expression)
            {
                case Atom atom:
                    CompileAtom(atom, typeContext);
                    break;
                case CallExpr call:
                    CompileCall(call, typeContext);
                    break;
                case BinaryOp binaryOp:
                    CompileBinaryOp(binaryOp, typeContext);
                    break;
                case UnaryOp unaryOp:
                    CompileUnaryOp(unaryOp, typeContext);
                    break;
                case ParenthesisExpr parenthesis:
                    CompileParenthesis(parenthesis, typeContext);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void CompileStatement(Statement statement, GlobalEmitionContext typeContext, out bool hasDirectReturn)
        {
            hasDirectReturn = false;
            switch (statement)
            {
                case VarDeclaration varDeclaration:
                    CompileVarDeclaration(varDeclaration, typeContext);
                    break;
                case Assignment assignment:
                    CompileAssignment(assignment, typeContext);
                    break;
                case ReturnStatement returnStatement:
                    hasDirectReturn = true;
                    CompileReturn(returnStatement, typeContext);
                    break;
                case IfStatement conditional:
                    CompileConditional(conditional, typeContext);
                    break;
                case WhileStatement loop:
                    CompileLoop(loop, typeContext);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void CompileLoop(WhileStatement loop, GlobalEmitionContext typeContext)
        {
            Emit currentMethod = typeContext.Methods[typeContext.Current].Emitter;
            
            Label loopStart = currentMethod.DefineLabel();
            Label endLoop = currentMethod.DefineLabel();

            currentMethod.MarkLabel(loopStart);
            CompileExpression(loop.Condition, typeContext);
            currentMethod.BranchIfFalse(endLoop);

            CompileBlock(loop.Body, typeContext, out bool hasDirectReturn);
            if (!hasDirectReturn)
            {
                currentMethod.Branch(loopStart);
            }
            
            currentMethod.MarkLabel(endLoop);
        }

        private static void CompileBlock(Block block, GlobalEmitionContext typeContext, out bool hasDirectReturn)
        {
            hasDirectReturn = false;
            foreach (var statement in block.Items)
            {
                CompileStatement(statement, typeContext, out bool statementHasDirectReturn);
                hasDirectReturn |= statementHasDirectReturn;
            }
        }

        private static void CompileReturn(ReturnStatement returnStatement, GlobalEmitionContext typeContext)
        {
            Emit currentMethod = typeContext.Methods[typeContext.Current].Emitter;
            CompileExpression(returnStatement.Value, typeContext);
            currentMethod.Convert<int>();
            currentMethod.Return();
        }

        private static void CompileVarDeclaration(VarDeclaration varDeclaration, GlobalEmitionContext typeContext)
        {
            LocalEmitionContext context = typeContext.Methods[typeContext.Current];

            if (context.Args.ContainsKey(varDeclaration.Name.FullName))
            {
                throw new Exception($"Argument {varDeclaration.Name} already declared");
            }
            else if (context.Locals.ContainsKey(varDeclaration.Name.FullName))
            {
                throw new Exception($"Variable {varDeclaration.Name} already declared");
            }

            context.Locals[varDeclaration.Name.FullName] = context.Emitter.DeclareLocal<int>();

            CompileExpression(varDeclaration.Value, typeContext);
            context.Emitter.StoreLocal(context.Locals[varDeclaration.Name.FullName]);
        }

        private static void CompileAssignment(Assignment assignment, GlobalEmitionContext typeContext)
        {
            LocalEmitionContext context = typeContext.Methods[typeContext.Current];

            CompileExpression(assignment.Value, typeContext);

            if (context.Locals.ContainsKey(assignment.Name.FullName))
            {
                context.Emitter.StoreLocal(context.Locals[assignment.Name.FullName]);
            } else if(context.Args.ContainsKey(assignment.Name.FullName))
            {
                context.Emitter.StoreArgument((ushort)context.Args[assignment.Name.FullName]);
            }
        }

        private static void CompileAtom(Atom tree, GlobalEmitionContext typeContext)
        {
            switch (tree)
            {
                case Identifier identifier:
                    CompileIdentifier(identifier, typeContext);
                    break;
                case Number number:
                    CompileNumber(number, typeContext);
                    break;
                case Boolean boolean:
                    CompileBoolean(boolean, typeContext);
                    break;
                default:
                    throw new Exception($"Unknown atom type {tree.GetType()}");
            }
        }

        private static void CompileConditional(IfStatement conditional, GlobalEmitionContext typeContext)
        {
            Emit currentMethod = typeContext.Methods[typeContext.Current].Emitter;

            Label condIsFalse = currentMethod.DefineLabel();
            Label conditionEnd = currentMethod.DefineLabel();

            CompileExpression(conditional.Condition, typeContext);
            currentMethod.BranchIfFalse(condIsFalse);
            CompileBlock(conditional.True, typeContext, out bool hasDirectReturn);
            if(!hasDirectReturn)
            {
                currentMethod.Branch(conditionEnd);
            }

            currentMethod.MarkLabel(condIsFalse);
            CompileBlock(conditional.False, typeContext, out _);

            currentMethod.MarkLabel(conditionEnd);
        }

        private static void CompileFunction(string @namespace, FunctionDef function, GlobalEmitionContext typeContext, bool logILCode)
        {
            string mangledName = Tools.Mangle(@namespace, function.Name);
            var currentContext = typeContext.Methods[mangledName];

            typeContext.Current = mangledName;

            var functionArgs = function.Args.Items.ToArray();
            for (int i = 0; i < functionArgs.Length; i++)
            {
                currentContext.Args[functionArgs[i].Name.FullName] = i;
            }

            switch (function.Body)
            {
                case Block block:
                    CompileBlock(block, typeContext, out bool hasDirectReturn);
                    if(hasDirectReturn) {
                        throw new Exception("method must have a return value");
                    }
                    break;
                case Expression expression:
                    CompileExpression(expression, typeContext);
                    currentContext.Emitter.Return();
                    break;
                default:
                    throw new Exception($"Unknown body type {function.Body.GetType()}");
            }

            string code = string.Empty;
            switch (functionArgs.Length)
            {
                case 0:
                    currentContext.Emitter.CreateDelegate<Func<int>>(out code);
                    break;
                case 1:
                    currentContext.Emitter.CreateDelegate<Func<int, int>>(out code);
                    break;
                case 2:
                    currentContext.Emitter.CreateDelegate<Func<int, int, int>>(out code);
                    break;
                case 3:
                    currentContext.Emitter.CreateDelegate<Func<int, int, int, int>>(out code);
                    break;
                case 4:
                    currentContext.Emitter.CreateDelegate<Func<int, int, int, int, int>>(out code);
                    break;
                case 5:
                    currentContext.Emitter.CreateDelegate<Func<int, int, int, int, int, int>>(out code);
                    break;
                case 6:
                    currentContext.Emitter.CreateDelegate<Func<int, int, int, int, int, int, int>>(out code);
                    break;
                case 7:
                    currentContext.Emitter.CreateDelegate<Func<int, int, int, int, int, int, int, int>>(out code);
                    break;

            }

            if(logILCode)
            {
                Console.WriteLine($"Start ==== function: {function.Name}====================================");
                Console.WriteLine(code);
                Console.WriteLine($"End   ==== function: {function.Name}====================================");
            }

        }

        private static GlobalEmitionContext PrepareGlobalContext(CompilationUnit compilationUnit, string @namespace = "")
        {
            var globalCtx = new GlobalEmitionContext() { 
                Methods = new() 
            };

            static void HandleMethodDefinition(string @namespace, FunctionDef function, GlobalEmitionContext typeContext)
            {
                string mangledName = Tools.Mangle(@namespace, function.Name);
                if (typeContext.Methods.ContainsKey(mangledName))
                {
                    throw new Exception($"Function {function.Name.FullName} already defined");
                }

                int argsCount = function.Args.Items.Length;
                typeContext.Methods[mangledName] = new LocalEmitionContext
                {
                    Locals = new(),
                    Args = new(),
                    Emitter = Emit.NewDynamicMethod(typeof(int), Enumerable.Range(0, argsCount).Select(_ => typeof(int)).ToArray(), mangledName)
                };
            }

            foreach (var library in compilationUnit.inludes)
            {
                foreach (var function in library.Value.Body)
                {
                    if (function is FunctionDef functionDef)
                    {
                        HandleMethodDefinition(library.Key, function, globalCtx);
                    }
                    else
                    {
                        throw new Exception($"Unknown tree type {function.GetType()}");
                    }
                }
            }

            foreach (var tree in compilationUnit.Body)
            {
                if (tree is FunctionDef function)
                {
                    HandleMethodDefinition(@namespace, function, globalCtx);
                }
                else
                {
                    throw new Exception($"Unknown tree type {tree.GetType()}");
                }
            }

            return globalCtx;
        }

        public static Func<int> Compile(CompilationUnit compilationUnit, string @namespace = "", bool logILCode = false)
        {
            var globalContext = PrepareGlobalContext(compilationUnit, @namespace);

            foreach (var library in compilationUnit.inludes)
            {
                globalContext.NameSpace = library.Key;
                foreach (var function in library.Value.Body)
                {
                    if (function is FunctionDef functionDef)
                    {
                        CompileFunction(library.Key, functionDef, globalContext, logILCode);
                    }
                    else
                    {
                        throw new Exception($"Unknown tree type {function.GetType()}");
                    }
                }
            }

            Emit mainEmitter = default;

            globalContext.NameSpace = @namespace;
            foreach (var tree in compilationUnit.Body)
            {
                if (tree is FunctionDef function)
                {
                    if(tree.Name.FullName == "Main")
                    {
                        mainEmitter = globalContext.Methods[tree.Name.FullName].Emitter;
                    }

                    CompileFunction(@namespace, function, globalContext, logILCode);
                }
                else
                {
                    throw new Exception($"Unknown tree type {tree.GetType()}");
                }
            }
            return mainEmitter!.CreateDelegate<Func<int>>();
        }
    }
}
