using iLang.SyntaxDefinitions;
using VirtualMachine.Example.Stack;
using Boolean = iLang.SyntaxDefinitions.Boolean;
using static VirtualMachine.Instructions.InstructionsExt.StacksExt;
using VirtualMachine.iLang.Compilers;
using String = System.String;
using VirtualMachine.Example.Register;
namespace iLang.Compilers.StacksCompiler
{
    public static class Compiler
    {
        class GlobalContext : FunctionContext<Stacks>
        {
            public override byte[] Collapse()

            {
                Dictionary<string, int> functionOffsets = new();

                Called.Add("Main");
                MachineCode.Add(Call, "Main");
                MachineCode.Add(Halt);


                functionOffsets["Main"] = 6;
                MachineCode.AddRange(Functions["Main"]);

                foreach (var function in Functions.Where(kvp => Called.Contains(kvp.Key)))
                {
                    if (function.Key == "Main") continue;
                    functionOffsets[function.Key] = MachineCode.Size;
                    MachineCode.AddRange(function.Value);
                }

                foreach (var instruction in MachineCode.Instruction)
                {
                    if (instruction.Op == Call && instruction.Operands[0] is Placeholder placeholder)
                    {
                        if (!functionOffsets.ContainsKey(placeholder.atom))
                        {
                            throw new Exception($"Function {placeholder.atom} not found");
                        }
                        instruction.Operands[0] = functionOffsets[placeholder.atom];
                    }
                }

                return MachineCode.Instruction.SelectMany(x =>
                {
                    if (x.Operands.Length > 0 && (x.Operands[0] is Value value))
                    {
                        return [x.Op.OpCode, .. BitConverter.GetBytes(value.Number)];
                    }
                    return new byte[] { x.Op.OpCode };
                }).ToArray();
            }
        }

        private static void CompileIdentifier(Identifier identifier_, Context<Stacks> context, GlobalContext  GlobalContext)
        {
            var identifier = identifier_ as Name;
            if (context.Variables.ContainsKey(identifier.FullName))
            {
                context.Bytecode.Add(Push, context.Variables[identifier.FullName]);
                context.Bytecode.Add(Push, 0);
                context.Bytecode.Add(Load);

            } else if (GlobalContext.GlobalVariables.ContainsKey(identifier.FullName))
            {
                context.Bytecode.Add(Push, GlobalContext.GlobalVariables[identifier.FullName]);
                context.Bytecode.Add(Push, 1);
                context.Bytecode.Add(Load);
            }
            else
            {
                throw new Exception($"Variable {identifier.FullName} not found");
            }
        }

        private static void CompileBoolean(Boolean boolean, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            context.Bytecode.Add(Push, boolean.Value ? 1 : 0);
        }

        private static void CompileNumber(Number number, Context<Stacks> context, GlobalContext  _)
        {
            context.Bytecode.Add(Push, (int)number.Value);
        }

        private static void CompileCall(CallExpression call, Context<Stacks> context, GlobalContext  GlobalContext)
        {
            foreach (var arg in call.Args.Items)
            {
                CompileExpression(arg, context, GlobalContext );
            }


            string funcName = Tools.Mangle(GlobalContext.CurrentNamespace, call.Function);
            GlobalContext.Called.Add(funcName);
            context.Bytecode.Add(Call, funcName);
        }

        private static void CompileBinaryOp(BinaryOperation binaryOp, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            CompileExpression(binaryOp.Right, context, GlobalContext );
            CompileExpression(binaryOp.Left, context, GlobalContext );
            switch (binaryOp.Op.Value)
            {
                case '+':
                    context.Bytecode.Add(Add);
                    break;
                case '-':
                    context.Bytecode.Add(Sub);
                    break;
                case '*':
                    context.Bytecode.Add(Mul);
                    break;
                case '/':
                    context.Bytecode.Add(Div);
                    break;
                case '%':
                    context.Bytecode.Add(Mod);
                    break;
                case '<':
                    context.Bytecode.Add(Lt);
                    break;
                case '>':
                    context.Bytecode.Add(Gt);
                    break;
                case '=':
                    context.Bytecode.Add(Eq);
                    break;
                case '&':
                    context.Bytecode.Add(And);
                    break;
                case '|':
                    context.Bytecode.Add(Or);
                    break;
                case '^':
                    context.Bytecode.Add(Xor);
                    break;
                default:
                    throw new Exception($"Unknown binary operator {binaryOp.Op.Value}");
            }
        }

        private static void CompileUnaryOp(UnaryOperation unaryOp, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            CompileExpression(unaryOp.Right, context, GlobalContext );
            switch (unaryOp.Op.Value)
            {
                case '+':
                    break;
                case '-':
                    context.Bytecode.Add(Push, -1);
                    context.Bytecode.Add(Mul);
                    break;
                case '!':
                    context.Bytecode.Add(Not);
                    break;
                default:
                    throw new Exception($"Unknown unary operator {unaryOp.Op.Value}");
            }
        }

        private static void CompileParenthesis(ParenthesisExpr parenthesis, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            CompileExpression(parenthesis.Body, context, GlobalContext );
        }

        private static void CompileExpression(Expression expression, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            switch (expression)
            {
                case Atom atom:
                    CompileAtom(atom, context, GlobalContext );
                    break;
                case CallExpression call:
                    CompileCall(call, context, GlobalContext );
                    break;
                case BinaryOperation binaryOp:
                    CompileBinaryOp(binaryOp, context, GlobalContext );
                    break;
                case UnaryOperation unaryOp:
                    CompileUnaryOp(unaryOp, context, GlobalContext );
                    break;
                case ParenthesisExpr parenthesis:
                    CompileParenthesis(parenthesis, context, GlobalContext );
                    break;
                default:
                    throw new Exception($"Unknown expression type {expression.GetType()}");
            }
        }

        private static void CompileStatement(Statement statement, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            switch (statement)
            {
                case IfStatement conditional:
                    CompileConditional(conditional, context, GlobalContext );
                    break;
                case VarDeclaration varDeclaration:
                    CompileVarDeclaration(varDeclaration, context, GlobalContext );
                    break;
                case Assignment assignment:
                    CompileAssignment(assignment, context, GlobalContext );
                    break;
                case ReturnStatement returnStatement:
                    CompileReturn(returnStatement, context, GlobalContext );
                    break;
                case WhileStatement loop:
                    CompileLoop(loop, context, GlobalContext );
                    break;
                default:
                    throw new Exception($"Unknown statement type {statement.GetType()}");
            }
        }

        private static void CompileLoop(WhileStatement loop, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            int current = context.Bytecode.Instruction.Count;
            int currentSize = context.Bytecode.Size;

            var snapshot = context.Snapshot;
            CompileBlock(loop.Body, snapshot, GlobalContext );
            var bodySliceSize = new Bytecode<Stacks>(snapshot.Bytecode.Instruction.Skip(context.Bytecode.Instruction.Count).ToList()).Size;

            CompileExpression(loop.Condition, context, GlobalContext );

            context.Bytecode.Add(Push, bodySliceSize + 6); // 24 + bodySlice.Size

            context.Bytecode.Add(Swap); // 8 + bodySlice.Size

            context.Bytecode.Add(Push, 0); // 13 + bodySlice.Size
            context.Bytecode.Add(Eq); // 8 + bodySlice.Size

            context.Bytecode.Add(CJump); // 7  + bodySlice.Size

            CompileBlock(loop.Body, context, GlobalContext ); // 6 + bodySlice.Size

            int jumpSize = 6 + context.Bytecode.Size - currentSize;
            context.Bytecode.Add(Push, -jumpSize); // 6
            context.Bytecode.Add(Jump); // 1
        }

        private static void CompileBlock(Block block, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            foreach (var statement in block.Items)
            {
                CompileStatement(statement, context, GlobalContext );
            }
        }

        private static void CompileReturn(ReturnStatement returnStatement, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            CompileExpression(returnStatement.Value, context, GlobalContext );
            context.Bytecode.Add(Ret);
        }

        private static void CompileVarDeclaration(VarDeclaration varDeclaration, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            if (context.Variables.ContainsKey(varDeclaration.Name.FullName))
            {
                throw new Exception($"Variable {varDeclaration.Name.FullName} already defined");
            }

            CompileExpression(varDeclaration.Value, context, GlobalContext );
            int address = varDeclaration.IsGlobal? GlobalContext.GlobalVariables.Count : context.Variables.Count;

            if (varDeclaration.IsGlobal)
            {
                GlobalContext.GlobalVariables[varDeclaration.Name.FullName] = address;
            }
            else
            {
                context.Variables[varDeclaration.Name.FullName] = address;
            }

            context.Bytecode.Add(Push, address);
            context.Bytecode.Add(Push, varDeclaration.IsGlobal? 1 : 0);
            context.Bytecode.Add(Store);
        }

        private static void CompileAssignment(Assignment assignment, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            var name = assignment.Name as Name;
            CompileExpression(assignment.Value, context, GlobalContext);
            if(context.Variables.ContainsKey(name.FullName))
            {
                context.Bytecode.Add(Push, context.Variables[name.FullName]);
                context.Bytecode.Add(Push, 0);
            } else if (GlobalContext.GlobalVariables.ContainsKey(name.FullName))
            {
                context.Bytecode.Add(Push, GlobalContext.GlobalVariables[name.FullName]);
                context.Bytecode.Add(Push, 1);
            }
            else
            {
                throw new Exception($"Variable {name.FullName} not found");
            }
            context.Bytecode.Add(Store);
        }

        private static void CompileAtom(Atom tree, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            switch (tree)
            {
                case Identifier identifier:
                    CompileIdentifier(identifier, context, GlobalContext );
                    break;
                case Number number:
                    CompileNumber(number, context, GlobalContext );
                    break;
                case Boolean boolean:
                    CompileBoolean(boolean, context, GlobalContext );
                    break;
                default:
                    throw new Exception($"Unknown atom type {tree.GetType()}");
            }
        }

        private static void CompileConditional(IfStatement conditional, Context<Stacks> context, GlobalContext  GlobalContext )
        {
            Context<Stacks> snapshot1 = context.Snapshot;
            CompileBlock(conditional.True, snapshot1, GlobalContext );
            var trueSlice = new Bytecode<Stacks>(snapshot1.Bytecode.Instruction[context.Bytecode.Instruction.Count..]);

            Context<Stacks> snapshot2 = context.Snapshot;
            CompileBlock(conditional.False, snapshot2, GlobalContext );
            var falseSlice = new Bytecode<Stacks>(snapshot2.Bytecode.Instruction[context.Bytecode.Instruction.Count..]);

            context.Bytecode.Add(Push, falseSlice.Size + 6); // 5
            CompileExpression(conditional.Condition, context, GlobalContext );
            context.Bytecode.Add(CJump); // 17

            CompileBlock(conditional.False, context, GlobalContext ); // 17 + falseSlice.Size

            context.Bytecode.Add(Push, trueSlice.Size); // 22 + falseSlice.Size
            context.Bytecode.Add(Jump); // 23 + falseSlice.Size

            CompileBlock(conditional.True, context, GlobalContext );
        }

        private static void CompileFunction(string @namespace, FunctionDefinition function, GlobalContext  GlobalContext )
        {
            string mangledName = Tools.Mangle(@namespace, function.Name);
            if (GlobalContext .Functions.ContainsKey(mangledName))
            {
                throw new Exception($"Function {function.Name.FullName} already defined");
            }

            var localContext = new Context<Stacks>(mangledName);

            var functionArgs = function.Args.Items.Reverse().ToArray();
            for (int i = 0; i < functionArgs.Length; i++)
            {
                localContext.Variables[functionArgs[i].Name.FullName] = i;
                localContext.Bytecode.Add(Push, i);
                localContext.Bytecode.Add(Push, 0);
                localContext.Bytecode.Add(Store);
            }

            switch (function.Body)
            {
                case Block block:
                    CompileBlock(block, localContext, GlobalContext );
                    break;
                case Expression expression:
                    CompileExpression(expression, localContext, GlobalContext );
                    localContext.Bytecode.Add(Ret);
                    break;
                default:
                    throw new Exception($"Unknown body type {function.Body.GetType()}");
            }

            GlobalContext .Functions[mangledName] = localContext.Bytecode;
        }

        public static byte[] Compile(CompilationUnit compilationUnit, string @namespace = "")
        {
            var GlobalContext  = new GlobalContext ();
            
            GlobalContext .CurrentNamespace = @namespace;
            foreach (var tree in compilationUnit.Body)
            {
                if (tree is FunctionDefinition function)
                {
                    CompileFunction(@namespace, function, GlobalContext );
                }
                else
                {
                    throw new Exception($"Unknown tree type {tree.GetType()}");
                }
            }

            foreach (var library in compilationUnit.FuncInludes)
            {
                GlobalContext .CurrentNamespace = library.Key;
                foreach (var function in library.Value.Body)
                {
                    if (function is FunctionDefinition functionDef)
                    {
                        CompileFunction(library.Key, functionDef, GlobalContext );
                    }
                    else
                    {
                        throw new Exception($"Unknown tree type {function.GetType()}");
                    }
                }
            }
            return GlobalContext .Collapse();
        }
    }
}
