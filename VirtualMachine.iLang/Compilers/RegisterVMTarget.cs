using iLang.SyntaxDefinitions;
using Boolean = iLang.SyntaxDefinitions.Boolean;
using static VirtualMachine.Instructions.InstructionsExt.RegistersExt;
using VirtualMachine.Example.Register;
using VirtualMachine.iLang.Compilers;
using VirtualMachine.Example.Stack;
namespace iLang.Compilers.RegisterTarget
{
    public static class Compiler
    {
        private static int eax = 0;
        private static int ebx = 1;
        private static int ecx = 2;
        private static int edx = 3;
        
        private static int fco = 5;
        private static int cjc = 4;
        private static int cjo = 6;
        private static int mof = 7;


        private class FunctionContext() : Context<Registers>(System.String.Empty)
        {
            public string CurrentNamespace { get; set; } = System.String.Empty;
            public Dictionary<string, Bytecode<Registers>> Functions{ get; } = new();
            public Bytecode<Registers> MachineCode { get; } = new(new List<Opcode<Registers>>());

            public byte[] Collapse()
            {
                
                Dictionary<string, int> functionOffsets = new();
                int offsetRegionSet = Functions.Count * 22 + 8;

                MachineCode.Add(Mov, eax, offsetRegionSet); // 6
                MachineCode.Add(Mov, mof, Tools.Address("Main")); // 6
                MachineCode.Add(Mov, cjo, 1); // 6
                MachineCode.Add(Store, eax, mof, cjo); // 1 + 1 + 1 + 1 = 4

                int acc = Functions["Main"].Size + offsetRegionSet;
                foreach (var function in Functions)
                {
                    if (function.Key == "Main") continue;
                    MachineCode.Add(Mov, eax, acc);
                    MachineCode.Add(Mov, mof, Tools.Address(function.Key));
                    MachineCode.Add(Mov, cjo, 1);
                    MachineCode.Add(Store, eax, mof, cjo);

                    acc += function.Value.Size;
                }

                MachineCode.Add(Mov, fco, "Main"); // 6
                MachineCode.Add(Call, fco); // 2

                functionOffsets["Main"] = offsetRegionSet;
                MachineCode.AddRange(Functions["Main"]);

                foreach (var function in Functions)
                {
                    if (function.Key == "Main") continue;
                    functionOffsets[function.Key] = MachineCode.Size;
                    MachineCode.AddRange(function.Value);
                }

                foreach (var instruction in MachineCode.Instruction)
                {
                    if (instruction.Op == Mov && instruction.Operands[1] is Placeholder placeholder)
                    {
                        if (!functionOffsets.ContainsKey(placeholder.atom))
                        {
                            throw new Exception($"Function {placeholder.atom} not found");
                        }
                        instruction.Operands[1] = functionOffsets[placeholder.atom];
                    }
                }
                

                return MachineCode.Instruction.SelectMany<Opcode<Registers>, byte>(x => {
                    switch(x.Op.Name)
                    {
                        case "Mov":
                        {
                            return [ x.Op.OpCode, (Byte)((Value)x.Operands[0]).Number, ..BitConverter.GetBytes(((Value)x.Operands[1]).Number) ];
                        }
                        default:
                        {
                            return [ x.Op.OpCode, .. x.Operands.Select<Operand, byte>(o => (byte)((Value)o).Number) ];
                        }

                    }
                }).ToArray();
            }
        }

        private static void CompileIdentifier(Identifier identifier, Context<Registers> context, FunctionContext functionContext)
        {
            if (context.Variables.ContainsKey(identifier.Value))
            {
                context.Bytecode.Add(Mov, cjo, 0);
                context.Bytecode.Add(Mov, mof, context.Variables[identifier.Value]);
                context.Bytecode.Add(Load, eax, mof, cjo);
            }
            else
            {
                throw new Exception($"Variable {identifier.Value} not found");
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
            // get stack frame size
            int stackframeSize = 16;

            // very very very bad workaround
            int argumentMemoryLocation = 1;
            foreach (var arg in call.Args.Items)
            {
                CompileExpression(arg, context, functionContext);
                context.Bytecode.Add(Mov, mof, stackframeSize + argumentMemoryLocation);
                context.Bytecode.Add(Mov, cjo, 0);
                context.Bytecode.Add(Store, eax, mof, cjo);
                
                argumentMemoryLocation += 1;
            }

            context.Bytecode.Add(Mov, fco, Tools.Mangle(functionContext.CurrentNamespace, call.Function));
            context.Bytecode.Add(Call, fco);
        }

        private static void CompileBinaryOp(BinaryOp binaryOp, Context<Registers> context, FunctionContext functionContext)
        {
            int leftMemoryLocation = context.Variables.Count;
            CompileExpression(binaryOp.Left, context, functionContext);
            context.Bytecode.Add(Mov, mof, leftMemoryLocation);
            context.Bytecode.Add(Mov, cjo, 0);
            context.Bytecode.Add(Store, eax, mof, cjo);
            int rightMemoryLocation = context.Variables.Count + 1;
            CompileExpression(binaryOp.Right, context, functionContext);
            context.Bytecode.Add(Mov, mof, rightMemoryLocation);
            context.Bytecode.Add(Mov, cjo, 0);
            context.Bytecode.Add(Store, eax, mof, cjo);

            context.Bytecode.Add(Mov, mof, leftMemoryLocation);
            context.Bytecode.Add(Load, eax, mof, cjo);
            context.Bytecode.Add(Mov, mof, rightMemoryLocation);
            context.Bytecode.Add(Load, ebx, mof, cjo);

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

            context.Bytecode.Add(binaryOpInstruction, eax, eax, ebx);
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
            int current = context.Bytecode.Instruction.Count;

            CompileBlock(loop.Body, context, functionContext);
            var bodySlice = new Bytecode<Registers>(context.Bytecode.Instruction[current..]);
            context.Bytecode.RemoveRange(current);

            CompileExpression(loop.Condition, context, functionContext);
            context.Bytecode.Add(Dup, cjc, eax);
            context.Bytecode.Add(Not, cjc, cjc);
            context.Bytecode.Add(Mov, eax, -1);
            context.Bytecode.Add(Eq, cjc, eax, cjc);

            // load function offset
            context.Bytecode.Add(Mov, edx, 0); // 6
            context.Bytecode.Add(Load, edx, edx, edx); // 4
            context.Bytecode.Add(Mov, ecx, context.Bytecode.Size + bodySlice.Size + 35); // 6
            context.Bytecode.Add(Add, cjo, ecx, edx); // 4

            context.Bytecode.Add(CJump, cjc, cjo); // 3
             
            context.Bytecode.AddRange(bodySlice);
            context.Bytecode.Add(Mov, ecx, loopStart); // 6

            context.Bytecode.Add(Mov, edx, 0); // 6
            context.Bytecode.Add(Load, edx, edx, edx); // 4
            context.Bytecode.Add(Add, cjo, ecx, edx); // 4
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
            if(context.Variables.ContainsKey(varDeclaration.Name.Value))
            {
                throw new Exception($"Variable {varDeclaration.Name} already declared");
            }

            context.Variables[varDeclaration.Name.Value] = context.Variables.Count;

            CompileExpression(varDeclaration.Value, context, functionContext);
            context.Bytecode.Add(Mov, mof, context.Variables[varDeclaration.Name.Value]);
            context.Bytecode.Add(Mov, cjo, 0);
            context.Bytecode.Add(Store, eax, mof, cjo);
        }

        private static void CompileAssignment(Assignment assignment, Context<Registers> context, FunctionContext functionContext)
        {
            if (!context.Variables.ContainsKey(assignment.Name.Value))
            {
                throw new Exception($"Variable {assignment.Name} not found");
            }

            CompileExpression(assignment.Value, context, functionContext);
            context.Bytecode.Add(Mov, mof, context.Variables[assignment.Name.Value]);
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
            context.Bytecode.Add(Dup, cjc, eax);

            int current = context.Bytecode.Instruction.Count;
            CompileBlock(conditional.True, context, functionContext);
            var trueSlice = new Bytecode<Registers>(context.Bytecode.Instruction[current..]);
            context.Bytecode.RemoveRange(current);

            CompileBlock(conditional.False, context, functionContext);
            var falseSlice = new Bytecode<Registers>(context.Bytecode.Instruction[current..]);
            context.Bytecode.RemoveRange(current);

            context.Bytecode.Add(Mov, edx, 0); // 6
            context.Bytecode.Add(Load, edx, edx, edx); // 4
            context.Bytecode.Add(Mov, ecx, context.Bytecode.Size + falseSlice.Size + 35); // 6
            context.Bytecode.Add(Add, cjo, ecx, edx); // 4

            context.Bytecode.Add(CJump, cjc, cjo); // 3

            context.Bytecode.AddRange(falseSlice); // falseSlice.Size

            context.Bytecode.Add(Mov, edx, 0); // 6
            context.Bytecode.Add(Load, edx, edx, edx); // 4
            context.Bytecode.Add(Mov, ecx, context.Bytecode.Size + trueSlice.Size + 12); // 6
            context.Bytecode.Add(Add, cjo, ecx, edx); // 4

            context.Bytecode.Add(Jump, cjo); // 2

            context.Bytecode.AddRange(trueSlice); // trueSlice.Size

        }

        private static void CompileFunction(string @namespace, FunctionDef function, FunctionContext functionContext)
        {
            string mangledName = Tools.Mangle(@namespace, function.Name);
            if (functionContext.Functions.ContainsKey(mangledName))
            {
                throw new Exception($"Function {function.Name.Value} already defined");
            }

            var localContext = new Context<Registers>(mangledName);

            int offsetIndex = Tools.Address(mangledName);
            localContext.Bytecode.Add(Mov, edx, offsetIndex);
            localContext.Bytecode.Add(Mov, ecx, 1);
            localContext.Bytecode.Add(Load, edx, edx, ecx);

            localContext.Bytecode.Add(Mov, eax, 0);
            localContext.Bytecode.Add(Store, edx, eax, eax);

            localContext.Variables["this"] = 0;

            var functionArgs = function.Args.Items.ToArray();
            for (int i = 0; i < functionArgs.Length; i++)
            {
                localContext.Variables[functionArgs[i].Value] = i + 1;
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
