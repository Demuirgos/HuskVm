using System.Reflection;
using VirtualMachine.Instruction;
using VirtualMachine.Processor;

namespace VirtualMachine.Example.Register
{
    namespace Instructions {
        public class Mov : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x01;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var Registers = state.Program[state.ProgramCounter++];
                // parse the next 4 bytes as an int
                var span = state.Program.AsSpan(state.ProgramCounter, 4);
                span.Reverse();
                Registerss[Registers] = BitConverter.ToInt32(span);
                state.ProgramCounter += 4;
                return vm;
            }
        }

        public class Add : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x02;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int Registers = span[0];
                int value1 = Registerss[span[1]];
                int value2 = Registerss[span[2]];
                state.ProgramCounter += 3;
                Registerss[Registers] = value1 + value2;
                return vm;
            }
        }

        public class Sub : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x03;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int Registers = span[0];
                int value1 = Registerss[span[1]];
                int value2 = Registerss[span[2]];
                state.ProgramCounter += 3;
                Registerss[Registers] = value1 - value2;
                return vm;
            }
        }

        public class Mul : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x04;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int Registers = span[0];
                int value1 = Registerss[span[1]];
                int value2 = Registerss[span[2]];
                state.ProgramCounter += 3;
                Registerss[Registers] = value1 * value2;
                return vm;
            }
        }

        public class Div : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x05;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int Registers = span[0];
                int value1 = Registerss[span[1]];
                int value2 = Registerss[span[2]];
                state.ProgramCounter += 3;
                Registerss[Registers] = value1 / value2;
                return vm;
            }
        }

        public class And : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x06;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int Registers = span[0];
                int value1 = Registerss[span[1]];
                int value2 = Registerss[span[2]];
                state.ProgramCounter += 3;
                Registerss[Registers] = value1 & value2;
                return vm;
            }
        }

        public class Or : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x07;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int Registers = span[0];
                int value1 = Registerss[span[1]];
                int value2 = Registerss[span[2]];
                state.ProgramCounter += 3;
                Registerss[Registers] = value1 | value2;
                return vm;
            }
        }

        public class Xor : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x08;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int Registers = span[0];
                int value1 = Registerss[span[1]];
                int value2 = Registerss[span[2]];
                state.ProgramCounter += 3;
                Registerss[Registers] = value1 ^ value2;
                return vm;
            }
        }

        public class Not : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x09;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 2);
                int Registers = span[0];
                int value = Registerss[span[1]];
                state.ProgramCounter += 2;
                Registerss[Registers] = ~value;
                return vm;
            }
        }

        public class Jump : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x06;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 1);
                int value = span[0];
                state.ProgramCounter = value;
                return vm;
            }
        }

        public class JumpIfZero : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x07;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
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
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 2);
                int Registers = span[0];
                int address = span[1];
                state.ProgramCounter += 2;
                Registerss[Registers] = state.Memory[address];
                return vm;
            }
        }

        public class Store : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x0b;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 2);
                int Registers = span[0];
                int address = span[1];
                state.ProgramCounter += 2;
                state.Memory[address] = Registerss[Registers];
                return vm;
            }
        }

        public class Dup : Instruction.Instruction<Registers> {
            public override byte OpCode { get; } = 0x0c;
            public override IVirtualMachine<Registers> Apply(IVirtualMachine<Registers> vm) {
                var state = vm.State;
                var Registerss = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 2);
                int src = span[0];
                int dest = span[1];
                state.ProgramCounter += 2;  
                Registerss[dest] = Registerss[src];
                return vm;
            }
        }

        public class Halt : Instruction.Instruction<Registers> {
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
            return string.Join(", ", Items);
        }
    }

    public record RegistersState() : IState<Registers> {
        public Registers Holder { get; set; } = new(8);
        public int ProgramCounter { get; set; } = 0;
        public int[] Memory { get; set; } = new int[16];
        public byte[] Program { get; set; }

        public override string ToString() {
            return $"ProgramCounter: {ProgramCounter}, Registerss: {Holder}, Memory: {string.Join(", ", Memory)}";
        }
    }

    public class VirtualMachine : BaseVirtualMachine<Registers> {
        public VirtualMachine() 
            : base(InstructionSet<Registers>.Opcodes, new RegistersState())
        {
        }

        public override string ToString() {
            return State.ToString();
        }
    }
}