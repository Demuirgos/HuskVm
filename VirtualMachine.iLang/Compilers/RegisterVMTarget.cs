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
using Microsoft.Diagnostics.Runtime;
using System.Collections;
using System.Diagnostics;
using Microsoft.Diagnostics.Tracing.StackSources;
using System.Buffers;
namespace iLang.Compilers.RegisterTarget
{
    public static class Compiler
    {
        public static class ToClr
        {
            internal static Bytecode<Registers> Simplify(Emit<Func<bool, int>> method, byte[] bytecode, out Dictionary<int, Label> labels, out Dictionary<int, (Label, Label)> functions)
            {
                labels = new();
                functions = new();
                var instructionMap = InstructionSet<Registers>.Opcodes.ToDictionary(instructions => instructions.OpCode);
                Bytecode<Registers> simplifiedBytecode = new([]);

                for (int j = 0; j < bytecode.Length; j++)
                {
                    var index = j;
                    var instruction = instructionMap[bytecode[j]];
                    var metadata = instruction.GetType().GetCustomAttribute<MetadataAttribute>();
                    var operands = new Operand[metadata.ImmediateSizes.Length];
                    for (int k = 0; k < metadata.ImmediateSizes.Length; k++)
                    {
                        operands[k] = metadata.ImmediateSizes[k] switch
                        {
                            1 => new Value(bytecode[j + 1]),
                            2 => new Value(BitConverter.ToInt16(bytecode, j + 1)),
                            4 => new Value(BitConverter.ToInt32(bytecode, j + 1)),
                            _ => throw new Exception("Invalid Immediate Size")
                        };
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

                            labels.TryAdd(target, method.DefineLabel());
                        }
                    }

                    if (instruction.OpCode == CJump.OpCode)
                    {
                        int falloff = index + 4 + 1 + 1;
                        labels.TryAdd(falloff, method.DefineLabel());


                        if (operands[1] is Value value)
                        {
                            int target = index + 1 + 4 + 1 + value.Number;

                            labels.TryAdd(target, method.DefineLabel());
                        }
                    }
                }

                return simplifiedBytecode;
            }

            internal static bool[] DeadcodeAnalysis(Bytecode<Registers> bytecode)
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
                            int falloff = j + 4 + 1 + 1;
                            jumpStack.Enqueue(falloff);

                            if (operands[1] is Value value)
                            {
                                int target = j + 1 + 4 + 1 + value.Number;
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

            internal static void EmitTrace(Emit<Func<bool, int>> method, Opcode<Registers> instruction, int pc, Local[] registers, Local memory, Local stackFrame) 
            {
                using Local traceLine = method.DeclareLocal<string>("traceLine");
                method.LoadConstant($"{pc}: {instruction} ");
                method.Call(typeof(Console).GetMethod("Write", [typeof(string)]));

                method.LoadConstant($"regs: [");
                method.Call(typeof(Console).GetMethod("Write", [typeof(string)]));
                foreach (var reg in registers)
                {
                    method.LoadLocal(reg);
                    method.Call(typeof(Console).GetMethod("Write", [typeof(int)]));
                    method.LoadConstant($", ");
                    method.Call(typeof(Console).GetMethod("Write", [typeof(string)]));
                }

                method.LoadConstant($"], l-mem: [");
                method.Call(typeof(Console).GetMethod("Write", [typeof(string)]));
                Label skipIfInDriver = method.DefineLabel();
                method.LoadLocal(stackFrame);
                method.BranchIfFalse(skipIfInDriver);

                for (int i = 0; i < Constants.frameSize; i++)
                {
                    using Local indexLcl = method.DeclareLocal<int>();

                    method.LoadLocal(memory);
                    method.LoadLocal(stackFrame);
                    method.LoadConstant(Constants.frameSize);
                    method.Multiply();
                    method.LoadConstant(i);
                    method.Add();
                    method.LoadElement<int>();
                    method.Call(typeof(Console).GetMethod("Write", [typeof(int)]));
                    method.LoadConstant($", ");
                    method.Call(typeof(Console).GetMethod("Write", [typeof(string)]));
                }

                method.MarkLabel(skipIfInDriver);

                method.LoadConstant("]\n");
                method.Call(typeof(Console).GetMethod("Write", [typeof(string)]));
            }
            public static Func<bool, int> ToMethodInfo(byte[] bytecodeInput)
            {
                Emit<Func<bool, int>> method = Emit<Func<bool, int>>.NewDynamicMethod(Guid.NewGuid().ToString(), doVerify: false, strictBranchVerification: true);

                Label returnTable = method.DefineLabel();
                Label exitLabel = method.DefineLabel();

                var bytecode = Simplify(method, bytecodeInput, out Dictionary<int, Label> labels, out Dictionary<int, (Label, Label) > functions);
                var reachabilityAnalysis = DeadcodeAnalysis(bytecode);
                
                using Local eax = method.DeclareLocal<int>("eax");
                using Local ebx = method.DeclareLocal<int>("ebx");
                using Local ecx = method.DeclareLocal<int>("ecx");
                using Local edx = method.DeclareLocal<int>("edx");

                using Local fco = method.DeclareLocal<int>("fco");
                using Local cjc = method.DeclareLocal<int>("cjc");
                using Local cjo = method.DeclareLocal<int>("cjo");
                using Local mof = method.DeclareLocal<int>("mof");

                using Local mem = method.DeclareLocal<int[]>("mem");
                method.Call(typeof(ArrayPool<int>).GetProperty(nameof(ArrayPool<int>.Shared), BindingFlags.Static | BindingFlags.Public).GetMethod);
                method.LoadConstant(1024);
                method.CallVirtual(typeof(ArrayPool<int>).GetMethod(nameof(ArrayPool<int>.Rent)));
                method.StoreLocal(mem);

                using Local cllstk = method.DeclareLocal<int[]>("cllstk");
                using Local stkHd = method.DeclareLocal<int>("stkHd");
                using Local currTrjt = method.DeclareLocal<int>("currTrjt");
                method.LoadConstant(32);
                method.NewArray<int>();
                method.StoreLocal(cllstk);

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

                for (int i = 0; i < bytecode.Instruction.Count; i++)
                {
                    var instruction = bytecode.Instruction[i];
                    int pc = bytecode.Pc(i);


                    if (!reachabilityAnalysis[i])
                    {
                        continue;
                    }

                    if (labels.ContainsKey(pc))
                    {
                        method.MarkLabel(labels[pc]);
                    }

                    Label skipTracing = method.DefineLabel();
                    method.LoadArgument(0);
                    method.BranchIfFalse(skipTracing);
                    EmitTrace(method, instruction, pc, [eax, ebx, ecx, edx, fco, cjc, cjo, mof], mem, stkHd);
                    method.MarkLabel(skipTracing);

                    if (instruction.Op.OpCode == Mov.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value)
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

                    if (instruction.Op.OpCode == Add.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value1 && instruction.Operands[2] is Value value2)
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

                    if (instruction.Op.OpCode == Sub.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value1 && instruction.Operands[2] is Value value2)
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

                    if (instruction.Op.OpCode == Mul.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value1 && instruction.Operands[2] is Value value2)
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

                    if (instruction.Op.OpCode == Div.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value1 && instruction.Operands[2] is Value value2)
                        {
                            method.LoadLocal(GetLocal(value2.Number));
                            method.LoadLocal(GetLocal(value1.Number));
                            method.Divide();
                            method.StoreLocal(GetLocal(destination.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op.OpCode == And.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value1 && instruction.Operands[2] is Value value2)
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

                    if (instruction.Op.OpCode == Or.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value1 && instruction.Operands[2] is Value value2)
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

                    if (instruction.Op.OpCode == Xor.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value1 && instruction.Operands[2] is Value value2)
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

                    if (instruction.Op.OpCode == Not.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value)
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
                        if (instruction.Operands[0] is Value condition && instruction.Operands[1] is Value target)
                        {
                            method.LoadLocal(GetLocal(condition.Number));
                            method.BranchIfTrue(labels[pc + 1 + 1 + 4 + target.Number]);
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op.OpCode == Load.OpCode)
                    {
                        if (instruction.Operands[0] is Value target && instruction.Operands[1] is Value address && instruction.Operands[2] is Value isGlobal)
                        {
                            Label globalTarget = method.DefineLabel();
                            Label localTarget = method.DefineLabel();
                            Label handlingReg = method.DefineLabel();

                            using Local offset = method.DeclareLocal<int>();
                            using Local correction = method.DeclareLocal<int>();
                            method.LoadLocal(GetLocal(address.Number));
                            method.StoreLocal(offset);

                            method.LoadLocal(GetLocal(isGlobal.Number));
                            method.BranchIfFalse(localTarget);

                            method.LoadConstant(Constants.globalFrame.Start.Value);
                            method.StoreLocal(correction);
                            method.Branch(handlingReg);

                            method.MarkLabel(localTarget);
                            method.LoadLocal(stkHd);
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
                            method.StoreLocal(GetLocal(target.Number));
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op.OpCode == Store.OpCode)
                    {
                        if (instruction.Operands[0] is Value source && instruction.Operands[1] is Value address && instruction.Operands[2] is Value isGlobal)
                        {
                            Label globalTarget = method.DefineLabel();
                            Label localTarget = method.DefineLabel();
                            Label handlingReg = method.DefineLabel();

                            using Local offset = method.DeclareLocal<int>();
                            using Local correction = method.DeclareLocal<int>();
                            method.LoadLocal(GetLocal(address.Number));
                            method.StoreLocal(offset);

                            method.LoadLocal(GetLocal(isGlobal.Number));
                            method.BranchIfFalse(localTarget);

                            method.LoadConstant(Constants.globalFrame.Start.Value);
                            method.StoreLocal(correction);
                            method.Branch(handlingReg);

                            method.MarkLabel(localTarget);
                            method.LoadLocal(stkHd);
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
                            method.LoadLocal(GetLocal(source.Number));
                            method.StoreElement<int>();
                        }
                        else
                        {
                            throw new Exception("Invalid operands");
                        }
                        continue;
                    }

                    if (instruction.Op.OpCode == Dup.OpCode)
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

                    if (instruction.Op.OpCode == Gt.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value1 && instruction.Operands[2] is Value value2)
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

                    if (instruction.Op.OpCode == Lt.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value1 && instruction.Operands[2] is Value value2)
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

                    if (instruction.Op.OpCode == Eq.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value1 && instruction.Operands[2] is Value value2)
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

                    if (instruction.Op.OpCode == Mod.OpCode)
                    {
                        if (instruction.Operands[0] is Value destination && instruction.Operands[1] is Value value1 && instruction.Operands[2] is Value value2)
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

                    if (instruction.Op.OpCode == Swap.OpCode)
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

                method.Call(typeof(ArrayPool<int>).GetProperty(nameof(ArrayPool<int>.Shared), BindingFlags.Static | BindingFlags.Public).GetMethod);
                method.LoadLocal(mem);
                method.LoadConstant(false);
                method.CallVirtual(typeof(ArrayPool<int>).GetMethod(nameof(ArrayPool<int>.Return)));

                method.LoadLocal(eax);
                method.Return();

                method.MarkLabel(returnTable);
                foreach(var functionLabels in functions)
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

        private const int eax = 0;
        private const int ebx = 1;
        private const int ecx = 2;
        private const int edx = 3;
        
        private const int fco = 5;
        private const int cjc = 4;
        private const int cjo = 6;
        private const int mof = 7;
        
        private static HashSet<string> TreeShaking(Dictionary<string, Bytecode<Registers>> functions, string function, HashSet<string> acc)
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

                return MachineCode.Instruction.SelectMany<Opcode<Registers>, byte>(x => {
                    if(x.Op.Name == Mov.Name || x.Op.Name == CJump.Name)
                    {
                        return [ x.Op.OpCode, (Byte)((Value)x.Operands[0]).Number, .. BitConverter.GetBytes(((Value)x.Operands[1]).Number) ];
                    } else if (x.Op.Name == Call.Name || x.Op.Name == Jump.Name)
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
            var calledFuncName = Tools.Mangle(functionContext.CurrentNamespace, call.Function);

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

            context.Bytecode.Add(Call, calledFuncName);
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

            context.Bytecode.Add(CJump, cjc, bodySlice.Size + 5); // 3
             
            CompileBlock(loop.Body, context, functionContext);
            
            context.Bytecode.Add(Jump, -(5 + context.Bytecode.Size - loopStart)); // 2
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

            context.Bytecode.Add(CJump, eax, falseSlice.Size + 5); // 3

            CompileBlock(conditional.False, context, functionContext);

            context.Bytecode.Add(Jump, trueSlice.Size); // 2

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
