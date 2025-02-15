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
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;
using Number = iLang.SyntaxDefinitions.Number;
using System.ComponentModel;
using System.Net;
using VirtualMachine.Example;
namespace iLang.Compilers.StacksCompiler
{
    public static class Compiler
    {
        public static class ToClr
        {
            internal static Bytecode<Stacks> Simplify(Emit<Func<int>> method, byte[] bytecode, out Dictionary<int, Label> labels, out Dictionary<int, (Label, Label)> functions)
            {
                labels = new();
                functions = new();

                var instructionMap = InstructionSet<Stacks>.Opcodes.ToDictionary(instructions => instructions.OpCode);
                Bytecode<Stacks> simplifiedBytecode = new([]);
                for (int j = 0; j < bytecode.Length; j++)
                {
                    var index = j;
                    var instruction = instructionMap[bytecode[j]];
                    var metadata = instruction.GetType().GetCustomAttribute<MetadataAttribute>();
                    var operands = new Operand[metadata.ImmediateSizes.Length];
                    for (int k = 0; k < metadata.ImmediateSizes.Length; k++)
                    {
                        operands[k] = new Value(BitConverter.ToInt32(bytecode[(j + 1)..(j + metadata.ImmediateSizes[k] + 1)]));
                        j += metadata.ImmediateSizes[k];
                    }

                    simplifiedBytecode.Add(instruction, operands);

                    if (instruction.OpCode == Call.OpCode)
                    {
                        if (operands[0] is Value value)
                        {
                            Label callTarget = method.DefineLabel();
                            Label returnTarget = method.DefineLabel();
                            labels.TryAdd(value.Number, callTarget);
                            labels.TryAdd(index + 4 + 1, returnTarget);

                            functions.TryAdd(value.Number, (callTarget, returnTarget));
                        }
                    }

                    if (instruction.OpCode == Jump.OpCode)
                    {
                        if (operands[0] is Value value)
                        {
                            int target = index + 4 + 1 + value.Number;

                            Console.WriteLine($"Jump to {target}");
                            labels.TryAdd(target, method.DefineLabel());
                        }
                    }

                    if (instruction.OpCode == CJump.OpCode)
                    {
                        int falloff = index + 4 + 1;
                        labels.TryAdd(falloff, method.DefineLabel());


                        if (operands[0] is Value value)
                        {
                            int target = index + 4 + 1 + value.Number;

                            Console.WriteLine($"CJump to {target}");
                            labels.TryAdd(target, method.DefineLabel());
                        }
                    }

                }

                return simplifiedBytecode;
            }

            internal static bool[] DeadcodeAnalysis(Bytecode<Stacks> bytecode)
            {
                Queue<int> jumpStack = new Queue<int>([0]);
                bool[] reachabilityAnalysis = new bool[bytecode.Instruction.Count];

                int bytecodeSize = bytecode.Size;
                
                HashSet<int> visited = new();

                while (jumpStack.TryDequeue(out int pc))
                {
                    if (visited.Contains(pc))
                    {
                        continue;
                    }


                    for (int j = pc; j < bytecodeSize;)
                    {
                        visited.Add(pc);

                        int index = bytecode.Index(j);
                        var metadata = bytecode.Instruction[index];
                        var instruction = metadata.Op;
                        var operands = metadata.Operands;


                        Console.WriteLine($"Analyzing {metadata}");

                        reachabilityAnalysis[index] = true;

                        if (instruction.OpCode == Call.OpCode)
                        {
                            if (operands[0] is Value value)
                            {
                                jumpStack.Enqueue(j + 4 + 1);
                                jumpStack.Enqueue(value.Number);

                                break;
                            }
                        }

                        if (instruction.OpCode == Jump.OpCode)
                        {
                            if (operands[0] is Value value)
                            {
                                int target = j + 4 + 1 + value.Number;

                                jumpStack.Enqueue(target);
                            }

                            break;
                        }

                        if (instruction.OpCode == CJump.OpCode)
                        {
                            int falloff = j + 4 + 1;
                            jumpStack.Enqueue(falloff);

                            if (operands[0] is Value value)
                            {
                                int target = j + 4 + 1 + value.Number;
                                jumpStack.Enqueue(target);
                            }

                            break;
                        }

                        if (instruction.OpCode == Ret.OpCode || instruction.OpCode == Halt.OpCode)
                        {
                            break;
                        }

                        j += metadata.Op.Size;
                    }
                }

                return reachabilityAnalysis;
            }

            public static Func<int> ToMethodInfo(byte[] bytecodeInput)
            {
                Emit<Func<int>> method = Emit<Func<int>>.NewDynamicMethod(Guid.NewGuid().ToString(), doVerify: true, strictBranchVerification: true);

                Label returnTable = method.DefineLabel();
                Label exitLabel = method.DefineLabel();

                var bytecode = Simplify(method, bytecodeInput, out Dictionary<int, Label> labels, out Dictionary<int, (Label, Label)> functions);
                var reachabilityAnalysis = DeadcodeAnalysis(bytecode);

                using Local mem = method.DeclareLocal<int[]>("mem");
                method.LoadConstant(1024);
                method.NewArray<int>();
                method.StoreLocal(mem);


                using Local cllstk = method.DeclareLocal<int[]>("cllstk");
                using Local stkHd = method.DeclareLocal<int>("stkHd");
                using Local currTrjt = method.DeclareLocal<int>("currTrjt");
                method.LoadConstant(1024);
                method.NewArray<int>();
                method.StoreLocal(cllstk);

                for (int i = 0; i < bytecode.Instruction.Count; i++)
                {
                    var instruction = bytecode.Instruction[i];
                    int pc = bytecode.Pc(i);

                    if (!reachabilityAnalysis[i])
                    {
                        Console.WriteLine($"{pc}: {instruction} (Deadcode)");
                        continue;
                    }

                    Console.WriteLine($"{pc}: {instruction}");
                    if (labels.ContainsKey(pc))
                    {
                        method.MarkLabel(labels[pc]);
                    }


                    using Local traceLine = method.DeclareLocal<string>("traceLine");
                    method.LoadConstant($"{pc}: {instruction}");
                    method.StoreLocal(traceLine);
                    method.LoadLocal(traceLine);
                    method.Call(typeof(Console).GetMethod("WriteLine", [typeof(string)]));

                    if (instruction.Op.Metadata.ArgumentCount > 0)
                    {
                        method.Duplicate();
                        method.Call(typeof(Console).GetMethod("WriteLine", [typeof(int)]));
                    }



                    if (instruction.Op.OpCode == Push.OpCode)
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

                    if (instruction.Op.OpCode == Add.OpCode)
                    {
                        method.Add();
                        continue;
                    }

                    if (instruction.Op.OpCode == Sub.OpCode)
                    {
                        method.Subtract();
                        continue;
                    }

                    if (instruction.Op.OpCode == Mul.OpCode)
                    {
                        method.Multiply();
                        continue;
                    }

                    if (instruction.Op.OpCode == Div.OpCode)
                    {
                        method.Divide();
                        continue;
                    }

                    if (instruction.Op.OpCode == And.OpCode)
                    {
                        method.And();
                        continue;
                    }

                    if (instruction.Op.OpCode == Or.OpCode)
                    {
                        method.Or();
                        continue;
                    }

                    if (instruction.Op.OpCode == Xor.OpCode)
                    {
                        method.Xor();
                        continue;
                    }

                    if (instruction.Op.OpCode == Not.OpCode)
                    {
                        method.Not();
                        continue;
                    }

                    if (instruction.Op.OpCode == Jump.OpCode)
                    {
                        if (instruction.Operands[0] is Value target)
                        {
                            method.Branch(labels[pc + 4 + 1 + target.Number]);
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op.OpCode == CJump.OpCode)
                    {
                        if (instruction.Operands[0] is Value target)
                        {
                            method.BranchIfTrue(labels[pc + 1 + 4 + target.Number]);
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op.OpCode == Load.OpCode)
                    {
                        Label globalTarget = method.DefineLabel();
                        Label localTarget = method.DefineLabel();
                        Label handlingReg = method.DefineLabel();

                        using Local offset = method.DeclareLocal<int>();
                        using Local correction = method.DeclareLocal<int>();
                        using Local isGlobal = method.DeclareLocal<int>();
                        method.StoreLocal(isGlobal);
                        method.StoreLocal(offset);

                        method.LoadLocal(isGlobal);
                        method.LoadConstant(0);
                        method.BranchIfEqual(localTarget);

                        method.LoadConstant(Constants.globalFrame.Start.Value);
                        method.StoreLocal(correction);
                        method.Branch(handlingReg);

                        method.MarkLabel(localTarget);
                        method.LoadLocal(stkHd);
                        method.LoadConstant(1);
                        method.Subtract();
                        method.LoadConstant(Constants.frameSize);
                        method.Multiply();
                        method.StoreLocal(correction);

                        method.MarkLabel(handlingReg);

                        method.LoadLocal(offset);
                        method.LoadLocal(correction);
                        method.Add();
                        method.StoreLocal(offset);

                        method.LoadLocal(mem);
                        method.LoadLocal(offset);
                        method.LoadElement<int>();
                        continue;
                    }

                    if (instruction.Op.OpCode == Store.OpCode)
                    {
                        Label globalTarget = method.DefineLabel();
                        Label localTarget = method.DefineLabel();
                        Label handlingReg = method.DefineLabel();

                        using Local offset = method.DeclareLocal<int>();
                        using Local correction = method.DeclareLocal<int>();
                        using Local isGlobal = method.DeclareLocal<int>();
                        using Local value = method.DeclareLocal<int>();

                        method.StoreLocal(isGlobal);
                        method.StoreLocal(offset);
                        method.StoreLocal(value);

                        method.LoadLocal(isGlobal);
                        method.LoadConstant(0);
                        method.BranchIfEqual(localTarget);


                        method.LoadConstant(Constants.globalFrame.Start.Value);
                        method.StoreLocal(correction);
                        method.Branch(handlingReg);

                        method.MarkLabel(localTarget);
                        method.LoadLocal(stkHd);
                        method.LoadConstant(1);
                        method.Subtract();
                        method.LoadConstant(Constants.frameSize);
                        method.Multiply();
                        method.StoreLocal(correction);

                        method.MarkLabel(handlingReg);

                        method.LoadLocal(offset);
                        method.LoadLocal(correction);
                        method.Add();
                        method.StoreLocal(offset);

                        method.LoadLocal(mem);
                        method.LoadLocal(offset);
                        method.LoadLocal(value);
                        method.StoreElement<int>();
                        continue;
                    }

                    if (instruction.Op.OpCode == Dup.OpCode)
                    {
                        method.Duplicate();
                        continue;
                    }

                    if (instruction.Op.OpCode == Gt.OpCode)
                    {
                        method.CompareGreaterThan();
                        continue;
                    }

                    if (instruction.Op.OpCode == Lt.OpCode)
                    {
                        method.CompareLessThan();
                        continue;
                    }

                    if (instruction.Op.OpCode == Eq.OpCode)
                    {
                        method.CompareEqual();
                        continue;
                    }

                    if (instruction.Op.OpCode == Mod.OpCode)
                    {
                        method.Remainder();
                        continue;
                    }

                    if (instruction.Op.OpCode == Swap.OpCode)
                    {
                        using Local a = method.DeclareLocal<int>();
                        using Local b = method.DeclareLocal<int>();

                        method.StoreLocal(a);
                        method.StoreLocal(b);

                        method.LoadLocal(a);
                        method.LoadLocal(b);
                        continue;
                    }

                    if (instruction.Op.OpCode == Call.OpCode)
                    {
                        if (instruction.Operands[0] is Value target)
                        {
                            method.LoadLocal(cllstk);
                            method.LoadLocal(stkHd);
                            method.LoadConstant(target.Number);
                            method.StoreElement<int>();

                            method.LoadLocal(stkHd);
                            method.LoadConstant(1);
                            method.Add();
                            method.StoreLocal(stkHd);

                            method.Branch(labels[target.Number]);
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op.OpCode == Ret.OpCode)
                    {
                        method.LoadLocal(cllstk);
                        method.LoadLocal(stkHd);
                        method.LoadConstant(1);
                        method.Subtract();
                        method.Duplicate();
                        method.StoreLocal(stkHd);

                        method.LoadElement<int>();
                        method.StoreLocal(currTrjt);

                        method.Branch(returnTable);
                        continue;
                    }

                    if (instruction.Op.OpCode == Halt.OpCode)
                    {
                        method.Branch(exitLabel);
                        continue;
                    }

                    throw new Exception($"Unknown instruction {instruction.Op}");
                }

                method.MarkLabel(exitLabel);
                method.Return();

                method.MarkLabel(returnTable);
                foreach (var functionLabels in functions)
                {
                    method.LoadLocal(currTrjt);
                    method.LoadConstant(functionLabels.Key);
                    method.BranchIfEqual(functionLabels.Value.Item2);
                }

                method.LoadConstant("Function not found");
                method.NewObject<Exception, string>();
                method.Throw();

                return method.CreateDelegate();
            }
        }
        private static HashSet<string> TreeShaking(Dictionary<string, Bytecode<Stacks>> functions, string function, HashSet<string> acc)
        {
            if (acc.Contains(function)) return acc;
            acc.Add(function);
            foreach (var instruction in functions[function].Instruction)
            {
                if (instruction.Op == Call && instruction.Operands[0] is FunctionName functionName)
                {
                    TreeShaking(functions, functionName.atom, acc);
                }
            }
            return acc;
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

                var calledFunctions = TreeShaking(Functions, "Main", []);

                foreach (var function in Functions)
                {
                    if (function.Key == "Main" || !calledFunctions.Contains(function.Key)) continue;
                    functionOffsets[function.Key] = MachineCode.Size;
                    MachineCode.AddRange(function.Value);
                }

                foreach (var instruction in MachineCode.Instruction)
                {
                    if (instruction.Op == Call && instruction.Operands[0] is FunctionName placeholder)
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
            var mangledName = Tools.Mangle(functionContext.CurrentNamespace, call.Function); 

            foreach (var arg in call.Args.Items)
            {
                CompileExpression(arg, context, functionContext);
            }
            context.Bytecode.Add(Call, mangledName);
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


            context.Bytecode.Add(Push, 0); // 13 + bodySlice.Size
            context.Bytecode.Add(Eq); // 8 + bodySlice.Size

            context.Bytecode.Add(CJump, bodySliceSize + 5); // 7  + bodySlice.Size

            CompileBlock(loop.Body, context, functionContext); // 6 + bodySlice.Size

            context.Bytecode.Add(Jump, -(5 + context.Bytecode.Size - currentSize)); // 1
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

            CompileExpression(conditional.Condition, context, functionContext);
            context.Bytecode.Add(CJump, falseSlice.Size + 5); // 17

            CompileBlock(conditional.False, context, functionContext); // 17 + falseSlice.Size

            context.Bytecode.Add(Jump, trueSlice.Size); // 23 + falseSlice.Size

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
