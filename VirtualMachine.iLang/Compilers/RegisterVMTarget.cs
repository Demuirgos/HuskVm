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


        private class GlobalContext() : FunctionContext<Registers>()
        {
            public override byte[] Collapse()
            {

                Dictionary<string, int> functionOffsets = new();

                Called.Add("Main");
                MachineCode.Add(Call, "Main"); // 5
                MachineCode.Add(Halt); // 1

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


                return MachineCode.Instruction.SelectMany<Opcode<Registers>, byte>(x => {
                    if (x.Op.Name == Mov.Name)
                    {
                        return [x.Op.OpCode, (Byte)((Value)x.Operands[0]).Number, .. BitConverter.GetBytes(((Value)x.Operands[1]).Number)];
                    }
                    else if (x.Op.Name == Call.Name)
                    {
                        return [x.Op.OpCode, .. BitConverter.GetBytes(((Value)x.Operands[0]).Number)];
                    }
                    else
                    {
                        return [x.Op.OpCode, .. x.Operands.Select<Operand, byte>(o => (byte)((Value)o).Number)];
                    }
                }).ToArray();
            }

        }

        private static void CompileIdentifier(Identifier identifier_, Context<Registers> context, GlobalContext  GlobalContext )
        {
            var identifier = identifier_ as Name;
            if (context.Variables.ContainsKey(identifier.FullName))
            {
                context.Bytecode.Add(Mov, cjo, 0);
                context.Bytecode.Add(Mov, mof, context.Variables[identifier.FullName]);
                context.Bytecode.Add(Load, eax, mof, cjo);
            } else if(GlobalContext .GlobalVariables.ContainsKey(identifier.FullName))
            {
                context.Bytecode.Add(Mov, cjo, 1);
                context.Bytecode.Add(Mov, mof, GlobalContext .GlobalVariables[identifier.FullName]);
                context.Bytecode.Add(Load, eax, mof, cjo);
            }
            else
            {
                throw new Exception($"Variable {identifier.FullName} not found");
            }
        }

        private static void CompileBoolean(Boolean boolean, Context<Registers> context, GlobalContext  GlobalContext )
        {
            context.Bytecode.Add(Mov, eax, boolean.Value ? 1 : 0);
        }

        private static void CompileNumber(Number number, Context<Registers> context, GlobalContext  _)
        {
            context.Bytecode.Add(Mov, eax, (int)number.Value);
        }

        private static void CompileCall(CallExpression call, Context<Registers> context, GlobalContext  GlobalContext )
        {
            
            // very very very bad workaround
            int argumentMemoryLocation = 0;
            foreach (var arg in call.Args.Items)
            {
                CompileExpression(arg, context, GlobalContext );
                context.Bytecode.Add(Mov, mof, Constants.frameSize + argumentMemoryLocation);
                context.Bytecode.Add(Mov, cjo, 0);
                context.Bytecode.Add(Store, eax, mof, cjo);
                
                argumentMemoryLocation += 1;
            }

            string funcName = Tools.Mangle(GlobalContext.CurrentNamespace, call.Function);
            GlobalContext.Called.Add(funcName);
            context.Bytecode.Add(Call, funcName);
        }

        private static void CompileBinaryOp(BinaryOperation binaryOp, Context<Registers> context, GlobalContext  GlobalContext )
        {
            CompileExpression(binaryOp.Left, context, GlobalContext );

            if(binaryOp.Right is Identifier rightId)
            {
                context.Bytecode.Add(Swap, eax, ebx);
                CompileIdentifier(rightId, context, GlobalContext );
            }
            else
            {
                context.Bytecode.Add(Mov, mof, context.Variables.Count);
                context.Bytecode.Add(Mov, cjo, 0);
                context.Bytecode.Add(Store, eax, mof, cjo);
            
                CompileExpression(binaryOp.Right, context, GlobalContext );
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

        private static void CompileUnaryOp(UnaryOperation unaryOp, Context<Registers> context, GlobalContext  GlobalContext )
        {
            CompileExpression(unaryOp.Right, context, GlobalContext );
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

        private static void CompileParenthesis(ParenthesisExpr parenthesis, Context<Registers> context, GlobalContext  GlobalContext )
        {
            CompileExpression(parenthesis.Body, context, GlobalContext );
        }

        private static void CompileExpression(Expression expression, Context<Registers> context, GlobalContext  GlobalContext )
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
                    throw new NotImplementedException();
            }
        }

        private static void CompileStatement(Statement statement, Context<Registers> context, GlobalContext  GlobalContext )
        {
            switch (statement)
            {
                case VarDeclaration varDeclaration:
                    CompileVarDeclaration(varDeclaration, context, GlobalContext );
                    break;
                case Assignment assignment:
                    CompileAssignment(assignment, context, GlobalContext );
                    break;
                case ReturnStatement returnStatement:
                    CompileReturn(returnStatement, context, GlobalContext );
                    break;
                case IfStatement conditional:
                    CompileConditional(conditional, context, GlobalContext );
                    break;
                case WhileStatement loop:
                    CompileLoop(loop, context, GlobalContext );
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void CompileLoop(WhileStatement loop, Context<Registers> context, GlobalContext  GlobalContext )
        {
            int loopStart = context.Bytecode.Size;

            var snapshot = context.Snapshot;
            CompileBlock(loop.Body, snapshot, GlobalContext );
            var bodySlice = new Bytecode<Registers>(snapshot.Bytecode.Instruction[context.Bytecode.Instruction.Count..]);

            CompileExpression(loop.Condition, context, GlobalContext );
            context.Bytecode.Add(Mov, ebx, 0);
            context.Bytecode.Add(Eq, cjc, eax, ebx);

            context.Bytecode.Add(Mov, cjo, bodySlice.Size + 8); // 6
            context.Bytecode.Add(CJump, cjc, cjo); // 3
             
            CompileBlock(loop.Body, context, GlobalContext );
            
            int jumpBack = 6 + 2 + context.Bytecode.Size - loopStart;
            context.Bytecode.Add(Mov, cjo, -jumpBack); // 6
            context.Bytecode.Add(Jump, cjo); // 2
        }

        private static void CompileBlock(Block block, Context<Registers> context, GlobalContext  GlobalContext )
        {
            foreach (var statement in block.Items)
            {
                CompileStatement(statement, context, GlobalContext );
            }
        }

        private static void CompileReturn(ReturnStatement returnStatement, Context<Registers> context, GlobalContext  GlobalContext )
        {
            CompileExpression(returnStatement.Value, context, GlobalContext );
            context.Bytecode.Add(Ret);
        }

        private static void CompileVarDeclaration(VarDeclaration varDeclaration, Context<Registers> context, GlobalContext  GlobalContext )
        {
            if(context.Variables.ContainsKey(varDeclaration.Name.FullName))
            {
                throw new Exception($"Variable {varDeclaration.Name} already declared");
            }

            CompileExpression(varDeclaration.Value, context, GlobalContext);
            int address = varDeclaration.IsGlobal ? GlobalContext.GlobalVariables.Count : context.Variables.Count;
            if (varDeclaration.IsGlobal)
            {
                GlobalContext.GlobalVariables[varDeclaration.Name.FullName] = address;
            }
            else
            {
                context.Variables[varDeclaration.Name.FullName] = address;
            }

            context.Bytecode.Add(Mov, mof, address);
            context.Bytecode.Add(Mov, cjo, varDeclaration.IsGlobal? 1 : 0);
            context.Bytecode.Add(Store, eax, mof, cjo);
        }

        private static void CompileAssignment(Assignment assignment, Context<Registers> context, GlobalContext  GlobalContext )
        {
            var name = assignment.Name as Name;
            CompileExpression(assignment.Value, context, GlobalContext );
            if (context.Variables.ContainsKey(name.FullName))
            {
                context.Bytecode.Add(Mov, mof, context.Variables[name.FullName]);
                context.Bytecode.Add(Mov, cjo, 0);
            } else if(GlobalContext .GlobalVariables.ContainsKey(name.FullName))
            {
                context.Bytecode.Add(Mov, mof, GlobalContext.GlobalVariables[name.FullName]);
                context.Bytecode.Add(Mov, cjo, 1);
            }
            else
            {
                throw new Exception($"Variable {name.FullName} not found");
            }
            context.Bytecode.Add(Store, eax, mof, cjo);
        }

        private static void CompileAtom(Atom tree, Context<Registers> context, GlobalContext  GlobalContext )
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

        private static void CompileConditional(IfStatement conditional, Context<Registers> context, GlobalContext  GlobalContext )
        {
            CompileExpression(conditional.Condition, context, GlobalContext );


            Context<Registers> snapshot1 = context.Snapshot;
            CompileBlock(conditional.True, snapshot1, GlobalContext );
            var trueSlice = new Bytecode<Registers>(snapshot1.Bytecode.Instruction[context.Bytecode.Instruction.Count..]);

            Context<Registers> snapshot2 = context.Snapshot;
            CompileBlock(conditional.False, snapshot2, GlobalContext );
            var falseSlice = new Bytecode<Registers>(snapshot2.Bytecode.Instruction[context.Bytecode.Instruction.Count..]);

            context.Bytecode.Add(Mov, cjo, falseSlice.Size + 8); // 6
            context.Bytecode.Add(CJump, eax, cjo); // 3

            CompileBlock(conditional.False, context, GlobalContext );

            context.Bytecode.Add(Mov, cjo, trueSlice.Size); // 6
            context.Bytecode.Add(Jump, cjo); // 2

            CompileBlock(conditional.True, context, GlobalContext );

        }

        private static void CompileFunction(string @namespace, FunctionDefinition function, GlobalContext  GlobalContext )
        {
            string mangledName = Tools.Mangle(@namespace, function.Name);
            if (GlobalContext .Functions.ContainsKey(mangledName))
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
