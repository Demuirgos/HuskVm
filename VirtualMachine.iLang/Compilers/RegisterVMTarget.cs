using iLang.SyntaxDefinitions;
using Boolean = iLang.SyntaxDefinitions.Boolean;
using static VirtualMachine.Instructions.InstructionsExt.RegistersExt;
using VirtualMachine.Example.Register;
using VirtualMachine.iLang.Compilers;
using VirtualMachine.Example;
using System.Reflection;
using Sigil;
using VirtualMachine.Instruction;
using Microsoft.Diagnostics.NETCore.Client;
namespace iLang.Compilers.RegisterTarget
{
    public static class Compiler
    {
        public static class ToClr
        {
            internal static Bytecode<Registers> Simplify(byte[] bytecode)
            {
                var instructionMap = InstructionSet<Registers>.Opcodes.ToDictionary(instructions => instructions.OpCode);
                Bytecode<Registers> simplifiedBytecode = new([]);
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

                using Local eax = method.DeclareLocal<int>("eax");
                using Local ebx = method.DeclareLocal<int>("ebx");
                using Local ecx = method.DeclareLocal<int>("ecx");
                using Local edx = method.DeclareLocal<int>("edx");

                using Local fco = method.DeclareLocal<int>("fco");
                using Local cjc = method.DeclareLocal<int>("cjc");
                using Local cjo = method.DeclareLocal<int>("cjo");
                using Local mof = method.DeclareLocal<int>("mof");

                using Local mem = method.DeclareLocal<int[]>("mem");

                method.LoadConstant(1024);
                method.NewArray<int>();
                method.StoreLocal(mem);

                Local GetLocal(int index) => index switch
                {
                    0 => eax,
                    1 => ebx,
                    2 => ecx,
                    3 => edx,
                    4 => fco,
                    5 => cjc,
                    6 => cjo,
                    7 => mof,
                    _ => throw new Exception($"Unknown register {index}")
                };

                Label[] labels = bytecode.Instruction.Select(x => method.DefineLabel()).ToArray();

                for (int i = 0; i < bytecode.Instruction.Count; i++)
                {
                    var instruction = bytecode.Instruction[i];
                    method.MarkLabel(labels[i]);

                    if (instruction.Op == Mov)
                    {
                        if (instruction.Operands[0] is Value value && instruction.Operands[1] is Value destination)
                        {
                            method.LoadConstant(value.Number);
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Add)
                    {
                        if (instruction.Operands[0] is Value value1 && instruction.Operands[1] is Value value2 && instruction.Operands[2] is Value destination)
                        {
                            method.LoadLocal(GetLocal(value1.Number));
                            method.LoadLocal(GetLocal(value2.Number));
                            method.Add();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Sub)
                    {
                        if (instruction.Operands[0] is Value value1 && instruction.Operands[1] is Value value2 && instruction.Operands[2] is Value destination)
                        {
                            method.LoadLocal(GetLocal(value1.Number));
                            method.LoadLocal(GetLocal(value2.Number));
                            method.Subtract();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Mul)
                    {
                        if (instruction.Operands[0] is Value value1 && instruction.Operands[1] is Value value2 && instruction.Operands[2] is Value destination)
                        {
                            method.LoadLocal(GetLocal(value1.Number));
                            method.LoadLocal(GetLocal(value2.Number));
                            method.Multiply();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Div)
                    {
                        if (instruction.Operands[0] is Value value1 && instruction.Operands[1] is Value value2 && instruction.Operands[2] is Value destination)
                        {
                            method.LoadLocal(GetLocal(value1.Number));
                            method.LoadLocal(GetLocal(value2.Number));
                            method.Divide();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == And)
                    {
                        if (instruction.Operands[0] is Value value1 && instruction.Operands[1] is Value value2 && instruction.Operands[2] is Value destination)
                        {
                            method.LoadLocal(GetLocal(value1.Number));
                            method.LoadLocal(GetLocal(value2.Number));
                            method.And();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Or)
                    {
                        if (instruction.Operands[0] is Value value1 && instruction.Operands[1] is Value value2 && instruction.Operands[2] is Value destination)
                        {
                            method.LoadLocal(GetLocal(value1.Number));
                            method.LoadLocal(GetLocal(value2.Number));
                            method.Or();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Xor)
                    {
                        if (instruction.Operands[0] is Value value1 && instruction.Operands[1] is Value value2 && instruction.Operands[2] is Value destination)
                        {
                            method.LoadLocal(GetLocal(value1.Number));
                            method.LoadLocal(GetLocal(value2.Number));
                            method.Xor();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Not)
                    {
                        if (instruction.Operands[0] is Value value && instruction.Operands[1] is Value destination)
                        {
                            method.LoadLocal(GetLocal(value.Number));
                            method.Not();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
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
                        if (instruction.Operands[0] is Value target && instruction.Operands[1] is Value address && instruction.Operands[2] is Value isGlobal)
                        {
                            Label sourceLocal = method.DefineLabel();
                            Label startHandling = method.DefineLabel();

                            method.LoadLocal(GetLocal(isGlobal.Number));
                            method.LoadConstant(0);
                            method.BranchIfEqual(sourceLocal);

                            method.LoadArgument(0);
                            method.Branch(startHandling);

                            method.MarkLabel(sourceLocal);
                            method.LoadLocal(mem);

                            method.MarkLabel(startHandling);
                            method.LoadLocal(GetLocal(address.Number));
                            method.LoadElement<int>();

                            method.StoreLocal(GetLocal(target.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Store)
                    {
                        if (instruction.Operands[0] is Value source && instruction.Operands[1] is Value address && instruction.Operands[2] is Value isGlobal)
                        {
                            Label targetLocal = method.DefineLabel();
                            Label startHandling = method.DefineLabel();

                            method.LoadLocal(GetLocal(isGlobal.Number));
                            method.LoadConstant(0);
                            method.BranchIfEqual(targetLocal);

                            method.LoadArgument(0);
                            method.Branch(startHandling);

                            method.MarkLabel(targetLocal);
                            method.LoadLocal(mem);

                            method.MarkLabel(startHandling);
                            method.LoadLocal(GetLocal(address.Number));
                            method.LoadLocal(GetLocal(source.Number));
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
                        if (instruction.Operands[0] is Value source && instruction.Operands[1] is Value destination)
                        {
                            method.LoadLocal(GetLocal(source.Number));
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Gt)
                    {
                        if (instruction.Operands[0] is Value value1 && instruction.Operands[1] is Value value2 && instruction.Operands[2] is Value destination)
                        {
                            method.LoadLocal(GetLocal(value1.Number));
                            method.LoadLocal(GetLocal(value2.Number));
                            method.CompareGreaterThan();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Lt)
                    {
                        if (instruction.Operands[0] is Value value1 && instruction.Operands[1] is Value value2 && instruction.Operands[2] is Value destination)
                        {
                            method.LoadLocal(GetLocal(value1.Number));
                            method.LoadLocal(GetLocal(value2.Number));
                            method.CompareLessThan();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Eq)
                    {
                        if (instruction.Operands[0] is Value value1 && instruction.Operands[1] is Value value2 && instruction.Operands[2] is Value destination)
                        {
                            method.LoadLocal(GetLocal(value1.Number));
                            method.LoadLocal(GetLocal(value2.Number));
                            method.CompareEqual();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op == Mod)
                    {
                        if (instruction.Operands[0] is Value value1 && instruction.Operands[1] is Value value2 && instruction.Operands[2] is Value destination)
                        {
                            method.LoadLocal(GetLocal(value1.Number));
                            method.LoadLocal(GetLocal(value2.Number));
                            method.Remainder();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
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
                        if (instruction.Operands[0] is Value source && instruction.Operands[1] is Value destination)
                        {
                            method.LoadLocal(GetLocal(source.Number));
                            method.LoadLocal(GetLocal(destination.Number));
                            method.StoreLocal(GetLocal(source.Number));
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
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

        private const int eax = 0;
        private const int ebx = 1;
        private const int ecx = 2;
        private const int edx = 3;
        
        private const int fco = 5;
        private const int cjc = 4;
        private const int cjo = 6;
        private const int mof = 7;


        private class FunctionContext() : Context<Registers>(System.String.Empty)
        {
            public string CurrentNamespace { get; set; } = System.String.Empty;
            public Dictionary<string, Bytecode<Registers>> Functions { get; } = new();
            public Bytecode<Registers> MachineCode { get; } = new(new List<Opcode<Registers>>());

            public byte[] Collapse()
            {
                
                Dictionary<string, int> functionOffsets = new();
                
                MachineCode.Add(Call, "Main"); // 5
                MachineCode.Add(Halt); // 1

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
                

                return MachineCode.Instruction.SelectMany<Opcode<Registers>, byte>(x => {
                    if(x.Op.Name == Mov.Name)
                    {
                        return [ x.Op.OpCode, (Byte)((Value)x.Operands[0]).Number, .. BitConverter.GetBytes(((Value)x.Operands[1]).Number) ];
                    } else if (x.Op.Name == Call.Name)
                    {
                        return [x.Op.OpCode, .. BitConverter.GetBytes(((Value)x.Operands[0]).Number)];
                    }
                    else
                    {
                        return [ x.Op.OpCode, .. x.Operands.Select<Operand, byte>(o => (byte)((Value)o).Number) ];
                    }   
                }).ToArray();
            }
        }

        private static void CompileIdentifier(Identifier identifier, Context<Registers> context, FunctionContext functionContext)
        {
            if (context.Variables.ContainsKey(identifier.FullName))
            {
                context.Bytecode.Add(Mov, cjo, 0);
                context.Bytecode.Add(Mov, mof, context.Variables[identifier.FullName]);
                context.Bytecode.Add(Load, eax, mof, cjo);
            }
            else
            {
                throw new Exception($"Variable {identifier.FullName} not found");
            }
        }

        private static void CompileBoolean(Boolean boolean, Context<Registers> context, FunctionContext functionContext)
        {
            context.Bytecode.Add(Mov, eax, boolean.Value ? 1 : 0);
        }

        private static void CompileNumber(Number number, Context<Registers> context, FunctionContext _)
        {
            context.Bytecode.Add(Mov, eax, (int)number.Value);
        }

        private static void CompileCall(CallExpr call, Context<Registers> context, FunctionContext functionContext)
        {
            
            // very very very bad workaround
            int argumentMemoryLocation = 0;
            foreach (var arg in call.Args.Items)
            {
                CompileExpression(arg, context, functionContext);
                context.Bytecode.Add(Mov, mof, Constants.frameSize + argumentMemoryLocation);
                context.Bytecode.Add(Mov, cjo, 0);
                context.Bytecode.Add(Store, eax, mof, cjo);
                
                argumentMemoryLocation += 1;
            }

            context.Bytecode.Add(Call, Tools.Mangle(functionContext.CurrentNamespace, call.Function));
        }

        private static void CompileBinaryOp(BinaryOp binaryOp, Context<Registers> context, FunctionContext functionContext)
        {
            CompileExpression(binaryOp.Left, context, functionContext);

            if(binaryOp.Right is Identifier rightId)
            {
                context.Bytecode.Add(Swap, eax, ebx);
                CompileIdentifier(rightId, context, functionContext);
            }
            else
            {
                context.Bytecode.Add(Mov, mof, context.Variables.Count);
                context.Bytecode.Add(Mov, cjo, 0);
                context.Bytecode.Add(Store, eax, mof, cjo);
            
                CompileExpression(binaryOp.Right, context, functionContext);
                context.Bytecode.Add(Mov, mof, context.Variables.Count);
                context.Bytecode.Add(Mov, cjo, 0);
                context.Bytecode.Add(Load, ebx, mof, cjo);
            }

            var binaryOpInstruction = binaryOp.Op.Value switch
            {
                '+' => Add,
                '-' => Sub,
                '*' => Mul,
                '/' => Div,
                '%' => Mod,
                '<' => Lt,
                '>' => Gt,
                '=' => Eq,
                '^' => Xor,
                '&' => And,
                '|' => Or,
                _ => throw new NotImplementedException()
            };

            context.Bytecode.Add(binaryOpInstruction, eax, ebx, eax);
        }

        private static void CompileUnaryOp(UnaryOp unaryOp, Context<Registers> context, FunctionContext functionContext)
        {
            CompileExpression(unaryOp.Right, context, functionContext);
            switch (unaryOp.Op.Value)
            {
                case '!':
                    context.Bytecode.Add(Not, eax, eax);
                    break;
                case '-':
                    context.Bytecode.Add(Mov, ebx, -1);
                    context.Bytecode.Add(Mul, eax, eax, ebx);
                    break;
                case '+':
                    // do nothing
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void CompileParenthesis(ParenthesisExpr parenthesis, Context<Registers> context, FunctionContext functionContext)
        {
            CompileExpression(parenthesis.Body, context, functionContext);
        }

        private static void CompileExpression(Expression expression, Context<Registers> context, FunctionContext functionContext)
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
                    throw new NotImplementedException();
            }
        }

        private static void CompileStatement(Statement statement, Context<Registers> context, FunctionContext functionContext)
        {
            switch (statement)
            {
                case VarDeclaration varDeclaration:
                    CompileVarDeclaration(varDeclaration, context, functionContext);
                    break;
                case Assignment assignment:
                    CompileAssignment(assignment, context, functionContext);
                    break;
                case ReturnStatement returnStatement:
                    CompileReturn(returnStatement, context, functionContext);
                    break;
                case IfStatement conditional:
                    CompileConditional(conditional, context, functionContext);
                    break;
                case WhileStatement loop:
                    CompileLoop(loop, context, functionContext);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void CompileLoop(WhileStatement loop, Context<Registers> context, FunctionContext functionContext)
        {
            int loopStart = context.Bytecode.Size;

            var snapshot = context.Snapshot;
            CompileBlock(loop.Body, snapshot, functionContext);
            var bodySlice = new Bytecode<Registers>(snapshot.Bytecode.Instruction[context.Bytecode.Instruction.Count..]);

            CompileExpression(loop.Condition, context, functionContext);
            context.Bytecode.Add(Mov, ebx, 0);
            context.Bytecode.Add(Eq, cjc, eax, ebx);

            context.Bytecode.Add(Mov, cjo, bodySlice.Size + 8); // 6
            context.Bytecode.Add(CJump, cjc, cjo); // 3
             
            CompileBlock(loop.Body, context, functionContext);
            
            int jumpBack = 6 + 2 + context.Bytecode.Size - loopStart;
            context.Bytecode.Add(Mov, cjo, -jumpBack); // 6
            context.Bytecode.Add(Jump, cjo); // 2
        }

        private static void CompileBlock(Block block, Context<Registers> context, FunctionContext functionContext)
        {
            foreach (var statement in block.Items)
            {
                CompileStatement(statement, context, functionContext);
            }
        }

        private static void CompileReturn(ReturnStatement returnStatement, Context<Registers> context, FunctionContext functionContext)
        {
            CompileExpression(returnStatement.Value, context, functionContext);
            context.Bytecode.Add(Ret);
        }

        private static void CompileVarDeclaration(VarDeclaration varDeclaration, Context<Registers> context, FunctionContext functionContext)
        {
            if(context.Variables.ContainsKey(varDeclaration.Name.FullName))
            {
                throw new Exception($"Variable {varDeclaration.Name} already declared");
            }

            context.Variables[varDeclaration.Name.FullName] = context.Variables.Count;

            CompileExpression(varDeclaration.Value, context, functionContext);
            context.Bytecode.Add(Mov, mof, context.Variables[varDeclaration.Name.FullName]);
            context.Bytecode.Add(Mov, cjo, 0);
            context.Bytecode.Add(Store, eax, mof, cjo);
        }

        private static void CompileAssignment(Assignment assignment, Context<Registers> context, FunctionContext functionContext)
        {
            if (!context.Variables.ContainsKey(assignment.Name.FullName))
            {
                throw new Exception($"Variable {assignment.Name} not found");
            }

            CompileExpression(assignment.Value, context, functionContext);
            context.Bytecode.Add(Mov, mof, context.Variables[assignment.Name.FullName]);
            context.Bytecode.Add(Mov, cjo, 0);
            context.Bytecode.Add(Store, eax, mof, cjo);
        }

        private static void CompileAtom(Atom tree, Context<Registers> context, FunctionContext functionContext)
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

        private static void CompileConditional(IfStatement conditional, Context<Registers> context, FunctionContext functionContext)
        {
            CompileExpression(conditional.Condition, context, functionContext);


            Context<Registers> snapshot1 = context.Snapshot;
            CompileBlock(conditional.True, snapshot1, functionContext);
            var trueSlice = new Bytecode<Registers>(snapshot1.Bytecode.Instruction[context.Bytecode.Instruction.Count..]);

            Context<Registers> snapshot2 = context.Snapshot;
            CompileBlock(conditional.False, snapshot2, functionContext);
            var falseSlice = new Bytecode<Registers>(snapshot2.Bytecode.Instruction[context.Bytecode.Instruction.Count..]);

            context.Bytecode.Add(Mov, cjo, falseSlice.Size + 8); // 6
            context.Bytecode.Add(CJump, eax, cjo); // 3

            CompileBlock(conditional.False, context, functionContext);

            context.Bytecode.Add(Mov, cjo, trueSlice.Size); // 6
            context.Bytecode.Add(Jump, cjo); // 2

            CompileBlock(conditional.True, context, functionContext);

        }

        private static void CompileFunction(string @namespace, FunctionDef function, FunctionContext functionContext)
        {
            string mangledName = Tools.Mangle(@namespace, function.Name);
            if (functionContext.Functions.ContainsKey(mangledName))
            {
                throw new Exception($"Function {function.Name.FullName} already defined");
            }

            var localContext = new Context<Registers>(mangledName);
            
            var functionArgs = function.Args.Items.ToArray();
            for (int i = 0; i < functionArgs.Length; i++)
            {
                localContext.Variables[functionArgs[i].Name.FullName] = i;
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
