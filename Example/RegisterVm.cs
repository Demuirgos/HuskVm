using System.Reflection;
using VirtualMachine.Instruction;
using VirtualMachine.Processor;

namespace VirtualMachine.Example.Register;
public static class Instructions {
    public class Mov : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x01;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 2, OutputCount = 1, ImmediateSizes = new[] { 1, 4 } };
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

    public class Add : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x02;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 3, OutputCount = 1, ImmediateSizes = new[] { 1, 1, 1 } };
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

    public class Sub : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x03;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 3, OutputCount = 1, ImmediateSizes = new[] { 1, 1, 1 } };
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

    public class Mul : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x04;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 3, OutputCount = 1, ImmediateSizes = new[] { 1, 1, 1 } };
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

    public class Div : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x05;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 3, OutputCount = 1, ImmediateSizes = new[] { 1, 1, 1 } };
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

    public class And : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x06;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 3, OutputCount = 1, ImmediateSizes = new[] { 1, 1, 1 } };
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

    public class Or : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x07;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 3, OutputCount = 1, ImmediateSizes = new[] { 1, 1, 1 } };
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

    public class Xor : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x08;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 3, OutputCount = 1, ImmediateSizes = new[] { 1, 1, 1 } };
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

    public class Not : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x09;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 2, OutputCount = 1, ImmediateSizes = new[] { 1, 1 } };
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

    public class Jump : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x06;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 1, OutputCount = 0, ImmediateSizes = new[] { 4 } };
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 1);
            int value = span[0];
            state.ProgramCounter = value;
            return vm;
        }
    }

    public class JumpIfZero : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x07;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 2, OutputCount = 0, ImmediateSizes = new[] { 1, 4 } };
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 2);
            int condition = span[0];
            int value = span[1];
            if(condition == 0) {
                state.ProgramCounter = value;
            } else {
                state.ProgramCounter += 2;
            }
            return vm;
        }
    }

    public class Load : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x0a;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 2, OutputCount = 1, ImmediateSizes = new[] { 1, 1 } };
        public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
            var state = vm.State;
            var Registers = state.Holder;
            var span = state.Program.AsSpan(state.ProgramCounter, 2);
            int Register = span[0];
            int address = span[1];
            state.ProgramCounter += 2;
            Registers[Register] = state.Memory[address];
            return vm;
        }
    }

    public class Store : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x0b;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 2, OutputCount = 0, ImmediateSizes = new[] { 1, 1 } };
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

    public class Dup : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0x0c;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 2, OutputCount = 1, ImmediateSizes = new[] { 1, 1 } };
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

    public class Halt : Instruction.Instruction<Registers> {
        public override byte OpCode { get; } = 0xff;
        public  override Metadata Metadata { get; } =new() { ArgumentCount = 0, OutputCount = 0, ImmediateSizes = new int[0] };
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