using VirtualMachine.Instruction;
using VirtualMachine.Processor;

namespace VirtualMachine.Example.Stack;
public static class Instructions {
    public class Push : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x01;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 1, OutputCount = 0, ImmediateSizes = new int[] { 4 }};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 4);
            int value = BitConverter.ToInt32(span);
            state.ProgramCounter += 4;
            stack.Push(value);
            return vm;
        }
    }

    public class Pop : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x02;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 0, OutputCount = 1, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            _ = stack.Pop();
            return vm;
        }
    }

    public class Add : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x03;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 2, OutputCount = 1, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() + stack.Pop());
            return vm;
        }
    }

    public class Sub : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x04;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 2, OutputCount = 1, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() - stack.Pop());
            return vm;
        }
    }

    public class Mul : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x05;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 2, OutputCount = 1, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() * stack.Pop());
            return vm;
        }
    }

    public class Div : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x06;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 2, OutputCount = 1, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() / stack.Pop());
            return vm;
        }
    }

    public class And : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x07;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 2, OutputCount = 1, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() & stack.Pop());
            return vm;
        }
    }

    public class Or : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x08;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 2, OutputCount = 1, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() | stack.Pop());
            return vm;
        }
    }

    public class Xor : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x09;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 2, OutputCount = 1, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() ^ stack.Pop());
            return vm;
        }
    }

    public class Not : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x0a;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 1, OutputCount = 1, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(~stack.Pop());
            return vm;
        }
    }

    public class Halt : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0xff;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 0, OutputCount = 0, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            vm.State.ProgramCounter = vm.State.Program.Length;
            return vm;
        }
    }

    public class Jump : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x0c;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 1, OutputCount = 0, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            state.ProgramCounter = stack.Pop();
            return vm;
        }
    }

    public class JumpIfZero : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x0d;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 2, OutputCount = 0, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            if (stack.Pop() == 0) {
                state.ProgramCounter = stack.Pop();
            }
            return vm;
        }
    }

    public class Load : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x0e;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 1, OutputCount = 1, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(state.Memory[stack.Pop()]);
            return vm;
        }
    }

    public class Store : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x0f;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 2, OutputCount = 0, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            state.Memory[stack.Pop()] = stack.Pop();
            return vm;
        }
    }

    public class Dup : Instruction.Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x10;
        public  override Metadata Metadata { get; } = new Metadata { ArgumentCount = 0, OutputCount = 1, ImmediateSizes = new int[0]};
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Peek());
            return vm;
        }
    }
}
public record StackState() : IState<Stack<int>> {
    public Stack<int> Holder { get; set; } = new Stack<int>();
    public int ProgramCounter { get; set; } = 0;
    public int[] Memory { get; set; } = new int[16];
    public byte[] Program { get; set; }

    public override string ToString() {
        return $"ProgramCounter: {ProgramCounter}, Stack: [{string.Join(", ", Holder)}], Memory: [{string.Join(", ", Memory)}]";
    }
}

public class VirtualMachine() : BaseVirtualMachine<Stack<int>>(InstructionSet<Stack<int>>.Opcodes, new StackState()) {
    public override string ToString() => State.ToString();
}