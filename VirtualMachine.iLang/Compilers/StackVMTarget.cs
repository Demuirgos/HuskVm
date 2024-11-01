using iLang.SyntaxDefinitions;
using VirtualMachine.Example.Stack;
using Boolean = iLang.SyntaxDefinitions.Boolean;
using static VirtualMachine.Instructions.InstructionsExt.StacksExt;
using VirtualMachine.iLang.Compilers;
using String = System.String;
using VirtualMachine.Example.Register;
using Microsoft.Diagnostics.NETCore.Client;
using Sigil;
using System.Reflection;
using VirtualMachine.Instruction;
namespace iLang.Compilers.StacksCompiler
{
    public static class Compiler
    {
        public static class ToClr
        {
            internal static Bytecode<Stacks> Simplify(byte[] bytecode)
            {
                var instructionMap = InstructionSet<Stacks>.Opcodes.ToDictionary(instructions => instructions.OpCode);
                Bytecode<Stacks> simplifiedBytecode = new([]);
                for (int j = 0; j < bytecode.Length; j++)
                {
                    var instruction = instructionMap[bytecode[j]];
                    var metadata = instruction.GetType().GetCustomAttribute<MetadataAttribute>();
                    var operands = new Operand[metadata.ImmediateSizes.Length];
                    for (int k = 0; k < metadata.ImmediateSizes.Length; k++)
                    {
                        operands[k] = new Value(BitConverter.ToInt32(bytecode[(j + 1)..(j + metadata.ImmediateSizes[k] + 1)]));
                        j += metadata.ImmediateSizes[k];
                    }

                    simplifiedBytecode.Add(instruction, operands);
                }

                return simplifiedBytecode;
            }

            public static MethodInfo ToMethodInfo(byte[] bytecodeInput)
            {
                Emit<Action<byte[]>> method = Emit<Action<byte[]>>.NewDynamicMethod(Guid.NewGuid().ToString(), doVerify: true, strictBranchVerification: true);


                var bytecode = Simplify(bytecodeInput);

                using Local mem = method.DeclareLocal<int[]>("mem");

                method.LoadConstant(1024);
                method.NewArray<int>();
                method.StoreLocal(mem);

                Label[] labels = bytecode.Instruction.Select(x => method.DefineLabel()).ToArray();

                for (int i = 0; i < bytecode.Instruction.Count; i++)
                {
                    var instruction = bytecode.Instruction[i];
                    method.MarkLabel(labels[i]);

                    if (instruction.Op == Push)
                    {
                        if (instruction.Operands[0] is Value value)
                        {
                            method.LoadConstant(value.Number);
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Add)
                    {
                        method.Add();
                        continue;
                    }

                    if (instruction.Op == Sub)
                    {
                        method.Subtract();
                        continue;
                    }

                    if (instruction.Op == Mul)
                    {
                        method.Multiply();
                        continue;
                    }

                    if (instruction.Op == Div)
                    {
                        method.Divide();
                        continue;
                    }

                    if (instruction.Op == And)
                    {
                        method.And();
                        continue;
                    }

                    if (instruction.Op == Or)
                    {
                        method.Or();
                        continue;
                    }

                    if (instruction.Op == Xor)
                    {
                        method.Xor();
                        continue;
                    }

                    if (instruction.Op == Not)
                    {
                        method.Not();
                        continue;
                    }

                    if (instruction.Op == Jump)
                    {
                        throw new UnsupportedCommandException("JUMP");
                        continue;
                    }

                    if (instruction.Op == CJump)
                    {
                        throw new UnsupportedCommandException("JUMPC");
                        continue;
                    }

                    if (instruction.Op == Load)
                    {
                        if (instruction.Operands[0] is Value address && instruction.Operands[1] is Value isGlobal)
                        {
                            Label sourceLocal = method.DefineLabel();
                            Label startHandling = method.DefineLabel();

                            method.LoadConstant(isGlobal.Number);
                            method.LoadConstant(0);
                            method.BranchIfEqual(sourceLocal);

                            method.LoadArgument(0);
                            method.Branch(startHandling);

                            method.MarkLabel(sourceLocal);
                            method.LoadLocal(mem);

                            method.MarkLabel(startHandling);

                            method.LoadConstant(address.Number);
                            method.LoadElement<int>();
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Store)
                    {
                        if (instruction.Operands[0] is Value address && instruction.Operands[1] is Value isGlobal)
                        {
                            Label sourceLocal = method.DefineLabel();
                            Label startHandling = method.DefineLabel();

                            method.LoadConstant(isGlobal.Number);
                            method.LoadConstant(0);
                            method.BranchIfEqual(sourceLocal);

                            method.LoadArgument(0);
                            method.Branch(startHandling);

                            method.MarkLabel(sourceLocal);
                            method.LoadLocal(mem);

                            method.MarkLabel(startHandling);

                            method.LoadConstant(address.Number);
                            method.LoadConstant(0);
                            method.StoreElement<int>();
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Dup)
                    {
                        method.Duplicate();
                        continue;
                    }

                    if (instruction.Op == Gt)
                    {
                        method.CompareGreaterThan();
                        continue;
                    }

                    if (instruction.Op == Lt)
                    {
                        method.CompareLessThan();
                        continue;
                    }

                    if (instruction.Op == Eq)
                    {
                        method.CompareEqual();
                        continue;
                    }

                    if (instruction.Op == Mod)
                    {
                        method.Remainder();
                        continue;
                    }

                    if (instruction.Op == Call)
                    {
                        throw new UnsupportedCommandException("CALL");
                        continue;
                    }

                    if (instruction.Op == Ret)
                    {
                        method.Return();
                        continue;
                    }

                    if (instruction.Op == Swap)
                    {
                        using Local a = method.DeclareLocal<int>();
                        using Local b = method.DeclareLocal<int>();

                        method.StoreLocal(a);
                        method.StoreLocal(b);

                        method.LoadLocal(a);
                        method.LoadLocal(b);
                        continue;
                    }

                    if (instruction.Op == Halt)
                    {
                        method.Return();
                        continue;
                    }

                    throw new Exception($"Unknown instruction {instruction.Op}");
                }

                method.Return();
                return method.CreateMethod();
            }
        }

        private class FunctionContext() : Context<Stacks>(String.Empty)
        {
            public string CurrentNamespace { get; set; } = System.String.Empty;
            public Dictionary<string, Bytecode<Stacks>> Functions { get; } = new();
            public Bytecode<Stacks> MachineCode { get; } = new(new List<Opcode<Stacks>>());

            public byte[] Collapse()
            {
                Dictionary<string, int> functionOffsets = new();

                MachineCode.Add(Call, "Main");
                MachineCode.Add(Halt);

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
                    if (instruction.Op == Call && instruction.Operands[0] is Placeholder placeholder)
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
            if (context.Variables.ContainsKey(identifier.FullName))
            {
                context.Bytecode.Add(Push, context.Variables[identifier.FullName]);
                context.Bytecode.Add(Push, 0);
                context.Bytecode.Add(Load);

            }
            else
            {
                throw new Exception($"Variable {identifier.FullName} not found");
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
            context.Bytecode.Add(Call, Tools.Mangle(functionContext.CurrentNamespace, call.Function));
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

            context.Bytecode.Add(Push, bodySliceSize + 6); // 24 + bodySlice.Size

            context.Bytecode.Add(Swap); // 8 + bodySlice.Size

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
            if (context.Variables.ContainsKey(varDeclaration.Name.FullName))
            {
                throw new Exception($"Variable {varDeclaration.Name.FullName} already defined");
            }

            context.Variables[varDeclaration.Name.FullName] = context.Variables.Count;

            CompileExpression(varDeclaration.Value, context, functionContext);
            context.Bytecode.Add(Push, context.Variables[varDeclaration.Name.FullName]);
            context.Bytecode.Add(Push, 0);
            context.Bytecode.Add(Store);
        }

        private static void CompileAssignment(Assignment assignment, Context<Stacks> context, FunctionContext functionContext)
        {
            if (!context.Variables.ContainsKey(assignment.Name.FullName))
            {
                throw new Exception($"Variable {assignment.Name.FullName} not found");
            }

            CompileExpression(assignment.Value, context, functionContext);
            context.Bytecode.Add(Push, context.Variables[assignment.Name.FullName]);
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
                    CompileBlock(block, localContext, functionContext);
                    break;
                case Expression expression:
                    CompileExpression(expression, localContext, functionContext);
                    localContext.Bytecode.Add(Ret);
                    break;
                default:
                    throw new Exception($"Unknown body type {function.Body.GetType()}");
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
