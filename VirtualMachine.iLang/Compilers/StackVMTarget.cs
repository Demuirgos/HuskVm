using iLang.SyntaxDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualMachine.Example.Stack;
using VirtualMachine.Instruction;
using Boolean = iLang.SyntaxDefinitions.Boolean;
using static VirtualMachine.Instructions.InstructionsExt.StacksExt;
namespace iLang.Compilers
{

    public static class Compilers
    {
        class Opcode(Instruction<Stacks> instruction, Operand Operand)
        {
            public override string ToString() => $"{instruction.Name} {Operand}";

            public Instruction<Stacks> Op { get; } = instruction;
            public Operand Operand { get; set; } = Operand;
        }
        record Operand;
        record Value(Number operand) : Operand
        {
            public override string ToString() => operand.Value.ToString();
        }

        record None : Operand
        {
            public override string ToString() => "";
        }
        record Placeholder(string atom) : Operand
        {
            public override string ToString() => atom;
        }
        record Bytecode(List<Opcode> Instruction)
        {
            public void Add(Instruction<Stacks> instruction) => Instruction.Add(new Opcode(instruction, new None()));
            public void Add(Instruction<Stacks> instruction, Number operand) => Instruction.Add(new Opcode(instruction, new Value(operand)));
            public void Add(Instruction<Stacks> instruction, Operand operand) => Instruction.Add(new Opcode(instruction, operand));

            public void AddRange(Bytecode bytecode) => Instruction.AddRange(bytecode.Instruction);

            public void RemoveRange(int start, int count) => Instruction.RemoveRange(start, count);
            public void RemoveRange(int start) => Instruction.RemoveRange(start, Instruction.Count - start);

            public int Size => Instruction.Sum(x => {
                if (x.Operand is Value) return 1 + 4;
                else if (x.Operand is Placeholder) return 1 + 4;
                return 1;
            });

            public override string ToString() => string.Join("\n", Instruction.Select((x, i) => $"{new Bytecode(Instruction.Slice(0, i)).Size} : {x.ToString()}"));
        }

        private class Context(string name)
        {
            public string Name { get; set; } = name;
            public Bytecode Bytecode { get; } = new(new List<Opcode>());
            public Dictionary<string, int> Variables { get; } = new();
        }

        private class FunctionContext() : Context(String.Empty)
        {
            public Dictionary<string, Bytecode> Functions { get; } = new();
            public Bytecode MachineCode { get; } = new(new List<Opcode>());

            public byte[] Collapse()
            {
                int AbsoluteValue(int value) => value < 0 ? -value : value;
                int Address(string name) => AbsoluteValue(name.GetHashCode() % 1024);

                Dictionary<string, int> functionOffsets = new();
                int offsetRegionSet = Functions.Count * 16 + 6;

                MachineCode.Add(Push, new Value(new Number(offsetRegionSet)));
                MachineCode.Add(Push, new Value(new Number(Address("Main"))));
                MachineCode.Add(Push, new Value(new Number(1)));
                MachineCode.Add(Store);

                int acc = Functions["Main"].Size + offsetRegionSet;
                foreach (var function in Functions)
                {
                    if (function.Key == "Main") continue;
                    MachineCode.Add(Push, new Value(new Number(acc)));
                    MachineCode.Add(Push, new Value(new Number(Address(function.Key))));
                    MachineCode.Add(Push, new Value(new Number(1)));
                    MachineCode.Add(Store);

                    acc += function.Value.Size;
                }

                MachineCode.Add(Push, new Placeholder("Main"));
                MachineCode.Add(Call);

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
                    if (instruction.Operand is Placeholder placeholder)
                    {
                        if (!functionOffsets.ContainsKey(placeholder.atom))
                        {
                            throw new Exception($"Function {placeholder.atom} not found");
                        }
                        instruction.Operand = new Value(new Number(functionOffsets[placeholder.atom]));
                    }
                }

                return MachineCode.Instruction.SelectMany(x => {
                    if (x.Operand is Value value)
                    {
                        return [x.Op.OpCode, .. BitConverter.GetBytes((int)value.operand.Value)];
                    }
                    return new byte[] { x.Op.OpCode };
                }).ToArray();
            }
        }

        private static void CompileIdentifier(Identifier identifier, Context context, FunctionContext functionContext)
        {
            if (context.Variables.ContainsKey(identifier.Value))
            {
                context.Bytecode.Add(Push, new Value(new Number(context.Variables[identifier.Value])));
                context.Bytecode.Add(Push, new Value(new Number(0)));
                context.Bytecode.Add(Load);

            }
            else
            {
                throw new Exception($"Variable {identifier.Value} not found");
            }
        }

        private static void CompileBoolean(Boolean boolean, Context context, FunctionContext functionContext)
        {
            context.Bytecode.Add(Push, boolean.Value ? new Number(1) : new Number(0));
        }

        private static void CompileNumber(Number number, Context context, FunctionContext _)
        {
            context.Bytecode.Add(Push, number);
        }

        private static void CompileCall(CallExpr call, Context context, FunctionContext functionContext)
        {
            foreach (var arg in call.Args.Items)
            {
                CompileExpression(arg, context, functionContext);
            }
            context.Bytecode.Add(Push, new Placeholder(((Identifier)call.Function).Value));
            context.Bytecode.Add(Call);
        }

        private static void CompileBinaryOp(BinaryOp binaryOp, Context context, FunctionContext functionContext)
        {
            CompileExpression(binaryOp.Right, context, functionContext);
            CompileExpression(binaryOp.Left, context, functionContext);
            switch (binaryOp.Op.op)
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
                    throw new Exception($"Unknown binary operator {binaryOp.Op.op}");
            }
        }

        private static void CompileUnaryOp(UnaryOp unaryOp, Context context, FunctionContext functionContext)
        {
            CompileExpression(unaryOp.Right, context, functionContext);
            switch (unaryOp.Op.op)
            {
                case '+':
                    break;
                case '-':
                    context.Bytecode.Add(Push, new Value(new Number(-1)));
                    context.Bytecode.Add(Mul);
                    break;
                case '!':
                    context.Bytecode.Add(Not);
                    break;
                default:
                    throw new Exception($"Unknown unary operator {unaryOp.Op.op}");
            }
        }

        private static void CompileParenthesis(ParenthesisExpr parenthesis, Context context, FunctionContext functionContext)
        {
            CompileExpression(parenthesis.Body, context, functionContext);
        }

        private static void CompileExpression(Expression expression, Context context, FunctionContext functionContext)
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

        private static void CompileStatement(Statement statement, Context context, FunctionContext functionContext)
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

        private static void CompileLoop(WhileStatement loop, Context context, FunctionContext functionContext)
        {
            int AbsoluteValue(int value) => value < 0 ? -value : value;
            int Address(string name) => AbsoluteValue(name.GetHashCode() % 1024);

            int current = context.Bytecode.Instruction.Count;
            int currentSize = context.Bytecode.Size;

            CompileBlock(loop.Body, context, functionContext);

            var bodySlice = new Bytecode(context.Bytecode.Instruction[current..]);
            context.Bytecode.RemoveRange(current);

            CompileExpression(loop.Condition, context, functionContext);
            context.Bytecode.Add(Push, new Value(new Number(14)));
            context.Bytecode.Add(Push, new Value(new Number(0)));
            context.Bytecode.Add(Store);

            int offsetIndex = Address(context.Name);

            context.Bytecode.Add(Push, new Value(new Number(offsetIndex)));
            context.Bytecode.Add(Push, new Value(new Number(1)));
            context.Bytecode.Add(Load);
            context.Bytecode.Add(Push, new Value(new Number(context.Bytecode.Size + bodySlice.Size + 42)));
            context.Bytecode.Add(Add);

            context.Bytecode.Add(Push, new Value(new Number(14)));
            context.Bytecode.Add(Push, new Value(new Number(0)));
            context.Bytecode.Add(Load);

            context.Bytecode.Add(Push, new Value(new Number(0)));
            context.Bytecode.Add(Eq);

            context.Bytecode.Add(CJump);

            context.Bytecode.AddRange(bodySlice);

            context.Bytecode.Add(Push, new Value(new Number(offsetIndex)));
            context.Bytecode.Add(Push, new Value(new Number(1)));
            context.Bytecode.Add(Load);
            context.Bytecode.Add(Push, new Value(new Number(currentSize)));
            context.Bytecode.Add(Add);

            context.Bytecode.Add(Jump);
        }

        private static void CompileBlock(Block block, Context context, FunctionContext functionContext)
        {
            foreach (var statement in block.Items)
            {
                CompileStatement(statement, context, functionContext);
            }
        }

        private static void CompileReturn(ReturnStatement returnStatement, Context context, FunctionContext functionContext)
        {
            CompileExpression(returnStatement.Value, context, functionContext);
            context.Bytecode.Add(Ret);
        }

        private static void CompileVarDeclaration(VarDeclaration varDeclaration, Context context, FunctionContext functionContext)
        {
            if (context.Variables.ContainsKey(varDeclaration.Name.Value))
            {
                throw new Exception($"Variable {varDeclaration.Name.Value} already defined");
            }

            context.Variables[varDeclaration.Name.Value] = context.Variables.Count;

            CompileExpression(varDeclaration.Value, context, functionContext);
            context.Bytecode.Add(Push, new Value(new Number(context.Variables[varDeclaration.Name.Value])));
            context.Bytecode.Add(Push, new Value(new Number(0)));
            context.Bytecode.Add(Store);
        }

        private static void CompileAssignment(Assignment assignment, Context context, FunctionContext functionContext)
        {
            if (!context.Variables.ContainsKey(assignment.Name.Value))
            {
                throw new Exception($"Variable {assignment.Name.Value} not found");
            }

            CompileExpression(assignment.Value, context, functionContext);
            context.Bytecode.Add(Push, new Value(new Number(context.Variables[assignment.Name.Value])));
            context.Bytecode.Add(Push, new Value(new Number(0)));
            context.Bytecode.Add(Store);
        }

        private static void CompileAtom(Atom tree, Context context, FunctionContext functionContext)
        {
            switch (tree)
            {
                case Identifier identifier:
                    if (!context.Variables.ContainsKey(identifier.Value))
                    {
                        context.Variables[identifier.Value] = context.Variables.Count;
                    }
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

        private static void CompileConditional(IfStatement conditional, Context context, FunctionContext functionContext)
        {
            int AbsoluteValue(int value) => value < 0 ? -value : value;
            int Address(string name) => AbsoluteValue(name.GetHashCode() % 1024);

            CompileExpression(conditional.Condition, context, functionContext);
            context.Bytecode.Add(Push, new Value(new Number(14)));
            context.Bytecode.Add(Push, new Value(new Number(0)));
            context.Bytecode.Add(Store);

            int current = context.Bytecode.Instruction.Count;

            CompileBlock(conditional.True, context, functionContext);
            var trueSlice = new Bytecode(context.Bytecode.Instruction[current..]);
            context.Bytecode.RemoveRange(current);

            CompileBlock(conditional.False, context, functionContext);
            var falseSlice = new Bytecode(context.Bytecode.Instruction[current..]);
            context.Bytecode.RemoveRange(current);

            int offsetIndex = Address(context.Name);

            context.Bytecode.Add(Push, new Value(new Number(offsetIndex)));
            context.Bytecode.Add(Push, new Value(new Number(1)));
            context.Bytecode.Add(Load);
            context.Bytecode.Add(Push, new Value(new Number(context.Bytecode.Size + falseSlice.Size + 36)));
            context.Bytecode.Add(Add);

            context.Bytecode.Add(Push, new Value(new Number(14)));
            context.Bytecode.Add(Push, new Value(new Number(0)));
            context.Bytecode.Add(Load);

            context.Bytecode.Add(CJump);

            context.Bytecode.AddRange(falseSlice);

            context.Bytecode.Add(Push, new Value(new Number(offsetIndex)));
            context.Bytecode.Add(Push, new Value(new Number(1)));
            context.Bytecode.Add(Load);
            context.Bytecode.Add(Push, new Value(new Number(context.Bytecode.Size + trueSlice.Size + 7)));
            context.Bytecode.Add(Add);
            context.Bytecode.Add(Jump);

            context.Bytecode.AddRange(trueSlice);
        }

        private static void CompileFunction(FunctionDef function, FunctionContext functionContext)
        {
            if (functionContext.Functions.ContainsKey(function.Name.Value))
            {
                throw new Exception($"Function {function.Name.Value} already defined");
            }

            var localContext = new Context(function.Name.Value);

            var functionArgs = function.Args.Items.Reverse().ToArray();
            for (int i = 0; i < functionArgs.Length; i++)
            {
                localContext.Variables[functionArgs[i].Value] = i;
                localContext.Bytecode.Add(Push, new Value(new Number(i)));
                localContext.Bytecode.Add(Push, new Value(new Number(0)));
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

            functionContext.Functions[function.Name.Value] = localContext.Bytecode;
        }

        public static byte[] Compile(CompilationUnit compilationUnit)
        {
            var functionContext = new FunctionContext();
            foreach (var tree in compilationUnit.Body)
            {
                if (tree is FunctionDef function)
                {
                    CompileFunction(function, functionContext);
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
