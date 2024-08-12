using iLang.SyntaxDefinitions;
using Boolean = iLang.SyntaxDefinitions.Boolean;
using static VirtualMachine.Instructions.InstructionsExt.RegistersExt;
using VirtualMachine.Example.Register;
using VirtualMachine.iLang.Compilers;
using VirtualMachine.Example;
namespace iLang.Compilers.RegisterTarget
{
    public static class Compiler
    {
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
