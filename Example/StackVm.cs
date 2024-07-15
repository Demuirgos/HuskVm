using VirtualMachine.Processor;

namespace VirtualMachine.Example
{
    namespace Instructions {
        public class Push : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x01;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                Console.WriteLine(vm);
                var state = vm.State;
                var stack = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 4);
                span.Reverse();
                int value = BitConverter.ToInt32(span);
                state.ProgramCounter += 4;
                stack.Push(value);
                Console.WriteLine(vm);
                return vm;
            }
        }

        public class Pop : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x02;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                var state = vm.State;
                var stack = state.Holder;
                state.Memory[stack.Pop()] = stack.Pop();
                return vm;
            }
        }

        public class Add : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x03;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                var state = vm.State;
                var stack = state.Holder;
                stack.Push(stack.Pop() + stack.Pop());
                return vm;
            }
        }

        public class Sub : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x04;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                var state = vm.State;
                var stack = state.Holder;
                stack.Push(stack.Pop() - stack.Pop());
                return vm;
            }
        }

        public class Mul : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x05;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                var state = vm.State;
                var stack = state.Holder;
                stack.Push(stack.Pop() * stack.Pop());
                return vm;
            }
        }

        public class Div : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x06;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                var state = vm.State;
                var stack = state.Holder;
                stack.Push(stack.Pop() / stack.Pop());
                return vm;
            }
        }

        public class And : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x07;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                var state = vm.State;
                var stack = state.Holder;
                stack.Push(stack.Pop() & stack.Pop());
                return vm;
            }
        }

        public class Or : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x08;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                var state = vm.State;
                var stack = state.Holder;
                stack.Push(stack.Pop() | stack.Pop());
                return vm;
            }
        }

        public class Xor : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x09;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                var state = vm.State;
                var stack = state.Holder;
                stack.Push(stack.Pop() ^ stack.Pop());
                return vm;
            }
        }

        public class Not : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x0a;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                var state = vm.State;
                var stack = state.Holder;
                stack.Push(~stack.Pop());
                return vm;
            }
        }

        public class Dup : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x0b;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                var state = vm.State;
                var stack = state.Holder;
                stack.Push(stack.Peek());
                return vm;
            }
        }

        public class Halt : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0xff;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                vm.State.ProgramCounter = vm.State.Program.Length;
                return vm;
            }
        }

        public class Jump : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x0c;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                var state = vm.State;
                var stack = state.Holder;
                state.ProgramCounter = stack.Pop();
                return vm;
            }
        }

        public class JumpIfZero : Instruction.Instruction<Stack<int>> {
            public override byte OpCode { get; } = 0x0d;
            public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
                var state = vm.State;
                var stack = state.Holder;
                if (stack.Pop() == 0) {
                    state.ProgramCounter = stack.Pop();
                }
                return vm;
            }
        }
    }

    public static class InstructionSet  
    {
        public static Instruction.Instruction<Stack<int>>[] Instructions = [
            new Instructions.Push(),
            new Instructions.Pop(),
            new Instructions.Add(),
            new Instructions.Sub(),
            new Instructions.Mul(),
            new Instructions.Div(),
            new Instructions.And(),
            new Instructions.Or(),
            new Instructions.Xor(),
            new Instructions.Not(),
            new Instructions.Dup(),
            new Instructions.Pop(),
            new Instructions.Halt(),
            new Instructions.Jump(),
            new Instructions.JumpIfZero()
        ];
    }
    public record StackState() : IState<Stack<int>> {
        public Stack<int> Holder { get; set; } = new Stack<int>();
        public int ProgramCounter { get; set; } = 0;
        public int[] Memory { get; set; } = new int[1024];
        public byte[] Program { get; set; }

        public override string ToString() {
            return $"ProgramCounter: {ProgramCounter}, Stack: {string.Join(", ", Holder)}";
        }
    }

    public record StackVirtualMachine : IVirtualMachine<Stack<int>> {
        public StackVirtualMachine(Instruction.Instruction<Stack<int>>[] instructionsSet) {
            int maxOpCode = instructionsSet.Max(i => i.OpCode);
            if(maxOpCode > 0xff) throw new Exception("Invalid OpCode");

            InstructionsSet = new Instruction.Instruction<Stack<int>>[maxOpCode + 1];
            foreach (var instruction in instructionsSet) {
                InstructionsSet[instruction.OpCode] = instruction;
            }
            State = new StackState();
        }
        public Instruction.Instruction<Stack<int>>[] InstructionsSet { get; set; }
        public IState<Stack<int>> State { get; set; }

        public override string ToString() {
            return State.ToString();
        }
    }
}