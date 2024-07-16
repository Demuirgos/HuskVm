using System.Reflection;
using VirtualMachine.Instruction;
using VirtualMachine.Processor;

namespace VirtualMachine.Example.Register;
public static class Instructions {

    [Metadata(2, 1, 1, 4)]
    public class Mov : Instruction<Registers> {
        public override byte OpCode { get; } = 0x01;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var Register = state.Program[state.ProgramCounter++];
            var span = state.Program.AsSpan(state.ProgramCounter, 4);
            Registers[Register] = BitConverter.ToInt32(span);
            state.ProgramCounter += 4;
            return vm;
        }
    }

    [Metadata(3, 1, 1, 1, 1)]
    public class Add : Instruction<Registers> {
        public override byte OpCode { get; } = 0x02;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 3);
            int Register = span[0];
            int value1 = Registers[span[1]];
            int value2 = Registers[span[2]];
            state.ProgramCounter += 3;
            Registers[Register] = value1 + value2;
            return vm;
        }
    }

    [Metadata(3, 1, 1, 1, 1)]
    public class Sub : Instruction<Registers> {
        public override byte OpCode { get; } = 0x03;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 3);
            int Register = span[0];
            int value1 = Registers[span[1]];
            int value2 = Registers[span[2]];
            state.ProgramCounter += 3;
            Registers[Register] = value1 - value2;
            return vm;
        }
    }

    [Metadata(3, 1, 1, 1, 1)]
    public class Mul : Instruction<Registers> {
        public override byte OpCode { get; } = 0x04;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 3);
            int Register = span[0];
            int value1 = Registers[span[1]];
            int value2 = Registers[span[2]];
            state.ProgramCounter += 3;
            Registers[Register] = value1 * value2;
            return vm;
        }
    }

    [Metadata(3, 1, 1, 1, 1)]
    public class Div : Instruction<Registers> {
        public override byte OpCode { get; } = 0x05;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 3);
            int Register = span[0];
            int value1 = Registers[span[1]];
            int value2 = Registers[span[2]];
            state.ProgramCounter += 3;
            Registers[Register] = value1 / value2;
            return vm;
        }
    }

    [Metadata(3, 1, 1, 1, 1)]
    public class And : Instruction<Registers> {
        public override byte OpCode { get; } = 0x06;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 3);
            int Register = span[0];
            int value1 = Registers[span[1]];
            int value2 = Registers[span[2]];
            state.ProgramCounter += 3;
            Registers[Register] = value1 & value2;
            return vm;
        }
    }

    [Metadata(3, 1, 1, 1, 1)]
    public class Or : Instruction<Registers> {
        public override byte OpCode { get; } = 0x07;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 3);
            int Register = span[0];
            int value1 = Registers[span[1]];
            int value2 = Registers[span[2]];
            state.ProgramCounter += 3;
            Registers[Register] = value1 | value2;
            return vm;
        }
    }

    [Metadata(3, 1, 1, 1, 1)]
    public class Xor : Instruction<Registers> {
        public override byte OpCode { get; } = 0x08;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 3);
            int Register = span[0];
            int value1 = Registers[span[1]];
            int value2 = Registers[span[2]];
            state.ProgramCounter += 3;
            Registers[Register] = value1 ^ value2;
            return vm;
        }
    }

    [Metadata(2, 1, 1, 1)]
    public class Not : Instruction<Registers> {
        public override byte OpCode { get; } = 0x09;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 2);
            int Register = span[0];
            int value = Registers[span[1]];
            state.ProgramCounter += 2;
            Registers[Register] = ~value;
            return vm;
        }
    }

    [Metadata(1, 0, 1)]
    public class Jump : Instruction<Registers> {
        public override byte OpCode { get; } = 0x06;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 1);
            int valueReg = span[0];
            state.ProgramCounter = Registers[valueReg];
            return vm;
        }
    }

    [Metadata(2, 0, 1, 1)]
    public class JumpIfZero : Instruction<Registers> {
        public override byte OpCode { get; } = 0x07;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 2);
            int condition = span[0];
            int value = span[1];
            if(Registers[condition] == 0) {
                state.ProgramCounter = Registers[value];
            } else {
                state.ProgramCounter += 2;
            }
            return vm;
        }
    }

    [Metadata(2, 1, 1, 1)]
    public class Load : Instruction<Registers> {
        public override byte OpCode { get; } = 0x0a;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 2);
            int Register = span[0];
            int addressReg = span[1];
            state.ProgramCounter += 2;
            Registers[Register] = state.Memory[Registers[addressReg]];
            return vm;
        }
    }

    [Metadata(2, 0, 1, 1)]
    public class Store : Instruction<Registers> {
        public override byte OpCode { get; } = 0x0b;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 2);
            int addressReg = span[0];
            int Register = span[1];
            state.ProgramCounter += 2;
            state.Memory[Registers[addressReg]] = Registers[Register];
            return vm;
        }
    }

    [Metadata(2, 1, 1, 1)]
    public class Dup : Instruction<Registers> {
        public override byte OpCode { get; } = 0x0c;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 2);
            int src = span[0];
            int dest = span[1];
            state.ProgramCounter += 2;  
            Registers[dest] = Registers[src];
            return vm;
        }
    }

    [Metadata(0, 0)]
    public class Halt : Instruction<Registers> {
        public override byte OpCode { get; } = 0xff;
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            vm.State.ProgramCounter = vm.State.Program.Length;
            return vm;
        }
    }
}

public record Registers(int Count) {
    public int[] Items { get; set; } = new int[Count];
    public int this[int index] {
        get => Items[index];
        set => Items[index] = value;
    }

    public override string ToString() {
        return "[" + string.Join(", ", Items) + "]";
    }
}

public record RegistersState() : IState<Registers> {
    public Registers Holder { get; set; } = new(8);
    public int ProgramCounter { get; set; } = 0;
    public int[] Memory { get; set; } = new int[16];
    public byte[] Program { get; set; }

    public override string ToString() {
        return $"ProgramCounter: {ProgramCounter}, Registers: {Holder}, Memory: [{string.Join(", ", Memory)}]";
    }
}

public class VirtualMachine() : BaseVirtualMachine<Registers>(InstructionSet<Registers>.Opcodes, new RegistersState()) {
    public override string ToString() => State.ToString();
}