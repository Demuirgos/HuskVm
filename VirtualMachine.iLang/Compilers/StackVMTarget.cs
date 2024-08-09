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
        private class FunctionContext() : Context<Stacks>(String.Empty)
        {
            public string CurrentNamespace { get; set; } = System.String.Empty;
            public Dictionary<string, Bytecode<Stacks>> Functions { get; } = new();
            public Bytecode<Stacks> MachineCode { get; } = new(new List<Opcode<Stacks>>());

            public byte[] Collapse()
            {
                Dictionary<string, int> functionOffsets = new();

                MachineCode.Add(Push, "Main");
                MachineCode.Add(Call);

                functionOffsets["Main"] = 6;
                MachineCode.AddRange(Functions["Main"]);

                foreach (var function in Functions)
                {
                    if (function.Key == "Main") continue;
                    functionOffsets[function.Key] = MachineCode.Size;
                    MachineCode.AddRange(function.Value);
                }

                foreach (var instruction in MachineCode.Instruction)
                {
                    if (instruction.Op == Push && instruction.Operands[0] is Placeholder placeholder)
                    {
                        if (!functionOffsets.ContainsKey(placeholder.atom))
                        {
                            throw new Exception($"Function {placeholder.atom} not found");
                        }
                        instruction.Operands[0] = functionOffsets[placeholder.atom];
                    }
                }

                return MachineCode.Instruction.SelectMany(x => {
                    if (x.Operands.Length > 0 && (x.Operands[0] is Value value))
                    {
                        return [x.Op.OpCode, .. BitConverter.GetBytes(value.Number)];
                    }
                    return new byte[] { x.Op.OpCode };
                }).ToArray();
            }
        }

        private static void CompileIdentifier(Identifier identifier, Context<Stacks> context, FunctionContext functionContext)
        {
            if (context.Variables.ContainsKey(identifier.Value))
            {
                context.Bytecode.Add(Push, context.Variables[identifier.Value]);
                context.Bytecode.Add(Push, 0);
                context.Bytecode.Add(Load);

            }
            else
            {
                throw new Exception($"Variable {identifier.Value} not found");
            }
        }

        private static void CompileBoolean(Boolean boolean, Context<Stacks> context, FunctionContext functionContext)
        {
            context.Bytecode.Add(Push, boolean.Value ? 1 : 0);
        }

        private static void CompileNumber(Number number, Context<Stacks> context, FunctionContext _)
        {
            context.Bytecode.Add(Push, (int)number.Value);
        }

        private static void CompileCall(CallExpr call, Context<Stacks> context, FunctionContext functionContext)
        {
            foreach (var arg in call.Args.Items)
            {
                CompileExpression(arg, context, functionContext);
            }
            context.Bytecode.Add(Push, Tools.Mangle(functionContext.CurrentNamespace, call.Function));
            context.Bytecode.Add(Call);
        }

        private static void CompileBinaryOp(BinaryOp binaryOp, Context<Stacks> context, FunctionContext functionContext)
        {
            CompileExpression(binaryOp.Right, context, functionContext);
            CompileExpression(binaryOp.Left, context, functionContext);
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

        private static void CompileUnaryOp(UnaryOp unaryOp, Context<Stacks> context, FunctionContext functionContext)
        {
            CompileExpression(unaryOp.Right, context, functionContext);
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

        private static void CompileParenthesis(ParenthesisExpr parenthesis, Context<Stacks> context, FunctionContext functionContext)
        {
            CompileExpression(parenthesis.Body, context, functionContext);
        }

        private static void CompileExpression(Expression expression, Context<Stacks> context, FunctionContext functionContext)
        {
            switch (expression)
            {
                case Atom atom:
                    CompileAtom(atom, context, functionContext);
                    break;
                case CallExpr call:
                    CompileCall(call, context, functionContext);
                    break;
                case BinaryOp binaryOp:
                    CompileBinaryOp(binaryOp, context, functionContext);
                    break;
                case UnaryOp unaryOp:
                    CompileUnaryOp(unaryOp, context, functionContext);
                    break;
                case ParenthesisExpr parenthesis:
                    CompileParenthesis(parenthesis, context, functionContext);
                    break;
                default:
                    throw new Exception($"Unknown expression type {expression.GetType()}");
            }
        }

        private static void CompileStatement(Statement statement, Context<Stacks> context, FunctionContext functionContext)
        {
            switch (statement)
            {
                case IfStatement conditional:
                    CompileConditional(conditional, context, functionContext);
                    break;
                case VarDeclaration varDeclaration:
                    CompileVarDeclaration(varDeclaration, context, functionContext);
                    break;
                case Assignment assignment:
                    CompileAssignment(assignment, context, functionContext);
                    break;
                case ReturnStatement returnStatement:
                    CompileReturn(returnStatement, context, functionContext);
                    break;
                case WhileStatement loop:
                    CompileLoop(loop, context, functionContext);
                    break;
                default:
                    throw new Exception($"Unknown statement type {statement.GetType()}");
            }
        }

        private static void CompileLoop(WhileStatement loop, Context<Stacks> context, FunctionContext functionContext)
        {
            int current = context.Bytecode.Instruction.Count;
            int currentSize = context.Bytecode.Size;

            var snapshot = context.Snapshot;
            CompileBlock(loop.Body, snapshot, functionContext);
            var bodySliceSize = new Bytecode<Stacks>(snapshot.Bytecode.Instruction.Skip(context.Bytecode.Instruction.Count).ToList()).Size;

            CompileExpression(loop.Condition, context, functionContext);
            context.Bytecode.Add(Push, 14); // 30 + bodySlice.Size
            context.Bytecode.Add(Push, 0); // 29 + bodySlice.Size
            context.Bytecode.Add(Store); // 25 + bodySlice.Size

            context.Bytecode.Add(Push, bodySliceSize + 6); // 24 + bodySlice.Size
            context.Bytecode.Add(Push, 14); // 19 + bodySlice.Size
            context.Bytecode.Add(Push, 0);
            context.Bytecode.Add(Load); // 14 + bodySlice.Size

            context.Bytecode.Add(Push, 0); // 13 + bodySlice.Size
            context.Bytecode.Add(Eq); // 8 + bodySlice.Size

            context.Bytecode.Add(CJump); // 7  + bodySlice.Size

            CompileBlock(loop.Body, context, functionContext); // 6 + bodySlice.Size

            int jumpSize = 6 + context.Bytecode.Size - currentSize;
            context.Bytecode.Add(Push, -jumpSize); // 6
            context.Bytecode.Add(Jump); // 1
        }

        private static void CompileBlock(Block block, Context<Stacks> context, FunctionContext functionContext)
        {
            foreach (var statement in block.Items)
            {
                CompileStatement(statement, context, functionContext);
            }
        }

        private static void CompileReturn(ReturnStatement returnStatement, Context<Stacks> context, FunctionContext functionContext)
        {
            CompileExpression(returnStatement.Value, context, functionContext);
            context.Bytecode.Add(Ret);
        }

        private static void CompileVarDeclaration(VarDeclaration varDeclaration, Context<Stacks> context, FunctionContext functionContext)
        {
            if (context.Variables.ContainsKey(varDeclaration.Name.Value))
            {
                throw new Exception($"Variable {varDeclaration.Name.Value} already defined");
            }

            context.Variables[varDeclaration.Name.Value] = context.Variables.Count;

            CompileExpression(varDeclaration.Value, context, functionContext);
            context.Bytecode.Add(Push, context.Variables[varDeclaration.Name.Value]);
            context.Bytecode.Add(Push, 0);
            context.Bytecode.Add(Store);
        }

        private static void CompileAssignment(Assignment assignment, Context<Stacks> context, FunctionContext functionContext)
        {
            if (!context.Variables.ContainsKey(assignment.Name.Value))
            {
                throw new Exception($"Variable {assignment.Name.Value} not found");
            }

            CompileExpression(assignment.Value, context, functionContext);
            context.Bytecode.Add(Push, context.Variables[assignment.Name.Value]);
            context.Bytecode.Add(Push, 0);
            context.Bytecode.Add(Store);
        }

        private static void CompileAtom(Atom tree, Context<Stacks> context, FunctionContext functionContext)
        {
            switch (tree)
            {
                case Identifier identifier:
                    CompileIdentifier(identifier, context, functionContext);
                    break;
                case Number number:
                    CompileNumber(number, context, functionContext);
                    break;
                case Boolean boolean:
                    CompileBoolean(boolean, context, functionContext);
                    break;
                default:
                    throw new Exception($"Unknown atom type {tree.GetType()}");
            }
        }

        private static void CompileConditional(IfStatement conditional, Context<Stacks> context, FunctionContext functionContext)
        {
            Context<Stacks> snapshot1 = context.Snapshot;
            CompileBlock(conditional.True, snapshot1, functionContext);
            var trueSlice = new Bytecode<Stacks>(snapshot1.Bytecode.Instruction[context.Bytecode.Instruction.Count..]);

            Context<Stacks> snapshot2 = context.Snapshot;
            CompileBlock(conditional.False, snapshot2, functionContext);
            var falseSlice = new Bytecode<Stacks>(snapshot2.Bytecode.Instruction[context.Bytecode.Instruction.Count..]);

            context.Bytecode.Add(Push, falseSlice.Size + 6); // 5
            CompileExpression(conditional.Condition, context, functionContext);
            context.Bytecode.Add(CJump); // 17

            CompileBlock(conditional.False, context, functionContext); // 17 + falseSlice.Size

            context.Bytecode.Add(Push, trueSlice.Size); // 22 + falseSlice.Size
            context.Bytecode.Add(Jump); // 23 + falseSlice.Size

            CompileBlock(conditional.True, context, functionContext);
        }

        private static void CompileFunction(string @namespace, FunctionDef function, FunctionContext functionContext)
        {
            string mangledName = Tools.Mangle(@namespace, function.Name);
            if (functionContext.Functions.ContainsKey(mangledName))
            {
                throw new Exception($"Function {function.Name.Value} already defined");
            }

            var localContext = new Context<Stacks>(mangledName);

            var functionArgs = function.Args.Items.Reverse().ToArray();
            for (int i = 0; i < functionArgs.Length; i++)
            {
                localContext.Variables[functionArgs[i].Value] = i;
                localContext.Bytecode.Add(Push, i);
                localContext.Bytecode.Add(Push, 0);
                localContext.Bytecode.Add(Store);
            }

            switch (function.Body)
            {
                case Block block:
                    CompileBlock(block, localContext, functionContext);
                    break;
                case Expression expression:
                    CompileExpression(expression, localContext, functionContext);
                    break;
                default:
                    throw new Exception($"Unknown body type {function.Body.GetType()}");
            }

            if (function.Name.Value == "Main")
            {
                localContext.Bytecode.Add(Halt);
            }

            functionContext.Functions[mangledName] = localContext.Bytecode;
        }

        public static byte[] Compile(CompilationUnit compilationUnit, string @namespace = "")
        {
            var functionContext = new FunctionContext();

            foreach (var library in compilationUnit.inludes)
            {
                functionContext.CurrentNamespace = library.Key;
                foreach (var function in library.Value.Body)
                {
                    if (function is FunctionDef functionDef)
                    {
                        CompileFunction(library.Key, functionDef, functionContext);
                    }
                    else
                    {
                        throw new Exception($"Unknown tree type {function.GetType()}");
                    }
                }
            }

            functionContext.CurrentNamespace = @namespace;
            foreach (var tree in compilationUnit.Body)
            {
                if (tree is FunctionDef function)
                {
                    CompileFunction(@namespace, function, functionContext);
                }
                else
                {
                    throw new Exception($"Unknown tree type {tree.GetType()}");
                }
            }

            return functionContext.Collapse();
        }
    }
}
