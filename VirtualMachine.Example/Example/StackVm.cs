using System.ComponentModel.DataAnnotations;
using VirtualMachine.Example;
using VirtualMachine.Instruction;
using VirtualMachine.Processor;

namespace VirtualMachine.Example.Stack;
public static class Instructions {
    
    [Metadata(1, 0, 4)]
    public class Push : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x01;
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

    [Metadata(0, 1)]
    public class Pop : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x02;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            _ = stack.Pop();
            return vm;
        }
    }

    [Metadata(2, 1)]
    public class Add : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x03;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() + stack.Pop());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public class Sub : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x04;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() - stack.Pop());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public class Mul : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x05;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() * stack.Pop());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public class Div : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x06;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() / stack.Pop());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public class And : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x07;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() & stack.Pop());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public class Or : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x08;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() | stack.Pop());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public class Xor : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x09;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() ^ stack.Pop());
            return vm;
        }
    }

    [Metadata(1, 1)]
    public class Not : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x0a;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(~stack.Pop());
            return vm;
        }
    }

    [Metadata(1, 0)]
    public class Jump : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x0c;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            state.ProgramCounter = stack.Pop();
            return vm;
        }
    }

    [Metadata(2, 0)]
    public class JumpIfZero : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x0d;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            var condition = stack.Pop() == 0;
            var destination = stack.Pop();
            if (condition) {
                state.ProgramCounter = destination;
            }
            return vm;
        }
    }

    [Metadata(1, 0)]
    public class Load : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x0e;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(state.Memory[stack.Pop()]);
            return vm;
        }
    }

    [Metadata(2, 0)]
    public class Store : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x0f;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            state.Memory[stack.Pop()] = stack.Pop();
            return vm;
        }
    }

    [Metadata(1, 2)]
    public class Dup : Instruction<Stack<int>> {
        public override byte OpCode { get; } = 0x10;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm) {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Peek());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public class Gt : Instruction<Stack<int>>
    {
        public override byte OpCode { get; } = 0x11;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm)
        {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() > stack.Pop() ? 1 : 0);
            return vm;
        }
    }

    [Metadata(2, 1)]
    public class Eq : Instruction<Stack<int>>
    {
        public override byte OpCode { get; } = 0x12;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm)
        {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() == stack.Pop() ? 1 : 0);
            return vm;
        }
    }

    [Metadata(2, 1)]
    public class Mod : Instruction<Stack<int>>
    {
        public override byte OpCode { get; } = 0x13;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm)
        {
            var state = vm.State;
            var stack = state.Holder;
            stack.Push(stack.Pop() % stack.Pop());
            return vm;
        }
    }

    [Metadata(0, 0)]
    public class Halt : Instruction<Stack<int>>
    {
        public override byte OpCode { get; } = 0xff;
        public override IVirtualMachine<Stack<int>> Apply(IVirtualMachine<Stack<int>> vm)
        {
            vm.State.ProgramCounter = vm.State.Program.Length;
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