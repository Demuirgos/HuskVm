using System.ComponentModel.DataAnnotations;
using VirtualMachine.Example;
using VirtualMachine.Instruction;
using VirtualMachine.Processor;

namespace VirtualMachine.Example.Stack;
public static class Instructions {
    
    [Metadata(1, 0, 4)]
    public partial class Push : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x01;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            var span = state.Program.AsSpan(state.ProgramCounter, 4);
            int value = BitConverter.ToInt32(span);
            state.ProgramCounter += 4;
            stack.Push(value);
            return vm;
        }
    }

    [Metadata(0, 1)]
    public partial class Pop : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x02;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            _ = stack.Pop();
            return vm;
        }
    }

    [Metadata(2, 1)]
    public partial class Add : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x03;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(stack.Pop() + stack.Pop());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public partial class Sub : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x04;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(stack.Pop() - stack.Pop());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public partial class Mul : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x05;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(stack.Pop() * stack.Pop());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public partial class Div : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x06;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(stack.Pop() / stack.Pop());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public partial class And : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x07;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(stack.Pop() & stack.Pop());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public partial class Or : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x08;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(stack.Pop() | stack.Pop());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public partial class Xor : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x09;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(stack.Pop() ^ stack.Pop());
            return vm;
        }
    }

    [Metadata(1, 1)]
    public partial class Not : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x0a;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(~stack.Pop());
            return vm;
        }
    }

    [Metadata(1, 0)]
    public partial class Jump : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x0c;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            state.ProgramCounter += stack.Pop();
            return vm;
        }
    }

    [Metadata(2, 0)]
    public partial class CJump : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x0d;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            var condition = stack.Pop() != 0;
            var offset = stack.Pop();
            if (condition) {
                state.ProgramCounter += offset;
            }
            return vm;
        }
    }

    [Metadata(1, 0)]
    public partial class Load : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x0e;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            int isGlobal = stack.Pop();
            int address = stack.Pop();
            // stackFrame [0, 512], global [512, 1024]
            Range stackFrame = 0..512;
            Range globalFrame = 0..512;
            int frameSize = 16;

            if (isGlobal != 0)
            {
                stack.Push(state.Memory[globalFrame.Start.Value + address]);
            }
            else
            {
                int offset = address + (state.Holder.Calls.Count - 1) * frameSize;
                stack.Push(state.Memory[offset]);
            }

            return vm;
        }
    }

    [Metadata(2, 0)]
    public partial class Store : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x0f;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            
            int isGlobal = stack.Pop();
            int address = stack.Pop();
            int value = stack.Pop();

            Range stackFrame = 0..512;
            Range globalFrame = 0..512;
            int frameSize = 16;

            if (isGlobal != 0)
            {
                state.Memory[globalFrame.Start.Value + address] = value;
            } else {
                int offset = address + (state.Holder.Calls.Count - 1) * frameSize;
                state.Memory[offset] = value;
            }

            return vm;
        }
    }

    [Metadata(1, 2)]
    public partial class Dup : Instruction<Stacks> {
        public override byte OpCode { get; } = 0x10;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm) {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(stack.Peek());
            return vm;
        }
    }

    [Metadata(2, 1)]
    public partial class Gt : Instruction<Stacks>
    {
        public override byte OpCode { get; } = 0x11;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm)
        {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(stack.Pop() > stack.Pop() ? 1 : 0);
            return vm;
        }
    }

    [Metadata(2, 1)]
    public partial class Lt : Instruction<Stacks>
    {
        public override byte OpCode { get; } = 0x16;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm)
        {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(stack.Pop() < stack.Pop() ? 1 : 0);
            return vm;
        }
    }

    [Metadata(2, 1)]
    public partial class Eq : Instruction<Stacks>
    {
        public override byte OpCode { get; } = 0x12;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm)
        {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(stack.Pop() == stack.Pop() ? 1 : 0);
            return vm;
        }
    }

    [Metadata(2, 1)]
    public partial class Mod : Instruction<Stacks>
    {
        public override byte OpCode { get; } = 0x13;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm)
        {
            var state = vm.State;
            var stack = state.Holder.Operands;
            stack.Push(stack.Pop() % stack.Pop());
            return vm;
        }
    }

    [Metadata(0, 0)]
    public partial class  Call : Instruction<Stacks>
    {
        public override byte OpCode { get; } = 0x14;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm)
        {
            var state = vm.State;
            var stack = state.Holder.Operands;
            state.Holder.Calls.Push(state.ProgramCounter);
            state.ProgramCounter = stack.Pop();
            return vm;
        }
    }

    [Metadata(0, 0)]
    public partial class Ret : Instruction<Stacks>
    {
        public override byte OpCode { get; } = 0x15;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm)
        {
            var state = vm.State;
            state.ProgramCounter = state.Holder.Calls.Pop();
            return vm;
        }
    }

    [Metadata(0, 0)]
    public partial class Halt : Instruction<Stacks>
    {
        public override byte OpCode { get; } = 0xff;
        public override IVirtualMachine<Stacks> Apply(IVirtualMachine<Stacks> vm)
        {
            vm.State.ProgramCounter = vm.State.Program.Length;
            return vm;
        }
    }
}

public class Stacks
{
    public Stack<int> Operands { get; set; } = new Stack<int>();
    public Stack<int> Calls { get; set; } = new Stack<int>();

    public override string ToString() => $"Operands: [{string.Join(", ", Operands)}], Calls: [{string.Join(", ", Calls)}]";
}

public record StackState() : IState<Stacks> {
    public Stacks Holder { get; set; } = new();
    public int ProgramCounter { get; set; } = 0;
    public int[] Memory { get; set; } = new int[1024];
    public byte[] Program { get; set; }

    public override string ToString() {
        return $"ProgramCounter: {ProgramCounter}, Stack: [{Holder}], Memory: [{string.Join(", ", Memory[0..32])}]";
    }
}

public class VirtualMachine() : BaseVirtualMachine<Stacks>(InstructionSet<Stacks>.Opcodes, new StackState()) {
    public override string ToString() => State.ToString();
}