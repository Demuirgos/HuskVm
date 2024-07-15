using VirtualMachine.Processor;

namespace VirtualMachine.Example.Register
{
    namespace Instructions {
        public class Mov : Instruction.Instruction<Register> {
            public override byte OpCode { get; } = 0x01;
            public override IVirtualMachine<Register> Apply(IVirtualMachine<Register> vm) {
                var state = vm.State;
                var registers = state.Holder;
                var register = state.Program[state.ProgramCounter++];
                // parse the next 4 bytes as an int
                var span = state.Program.AsSpan(state.ProgramCounter, 4);
                span.Reverse();
                registers[register] = BitConverter.ToInt32(span);
                state.ProgramCounter += 4;
                return vm;
            }
        }

        public class Add : Instruction.Instruction<Register> {
            public override byte OpCode { get; } = 0x02;
            public override IVirtualMachine<Register> Apply(IVirtualMachine<Register> vm) {
                var state = vm.State;
                var registers = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int register = span[0];
                int value1 = registers[span[1]];
                int value2 = registers[span[2]];
                state.ProgramCounter += 3;
                registers[register] = value1 + value2;
                return vm;
            }
        }

        public class Sub : Instruction.Instruction<Register> {
            public override byte OpCode { get; } = 0x03;
            public override IVirtualMachine<Register> Apply(IVirtualMachine<Register> vm) {
                var state = vm.State;
                var registers = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int register = span[0];
                int value1 = registers[span[1]];
                int value2 = registers[span[2]];
                state.ProgramCounter += 3;
                registers[register] = value1 - value2;
                return vm;
            }
        }

        public class Mul : Instruction.Instruction<Register> {
            public override byte OpCode { get; } = 0x04;
            public override IVirtualMachine<Register> Apply(IVirtualMachine<Register> vm) {
                var state = vm.State;
                var registers = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int register = span[0];
                int value1 = registers[span[1]];
                int value2 = registers[span[2]];
                state.ProgramCounter += 3;
                registers[register] = value1 * value2;
                return vm;
            }
        }

        public class Div : Instruction.Instruction<Register> {
            public override byte OpCode { get; } = 0x05;
            public override IVirtualMachine<Register> Apply(IVirtualMachine<Register> vm) {
                var state = vm.State;
                var registers = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int register = span[0];
                int value1 = registers[span[1]];
                int value2 = registers[span[2]];
                state.ProgramCounter += 3;
                registers[register] = value1 / value2;
                return vm;
            }
        }

        public class And : Instruction.Instruction<Register> {
            public override byte OpCode { get; } = 0x06;
            public override IVirtualMachine<Register> Apply(IVirtualMachine<Register> vm) {
                var state = vm.State;
                var registers = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int register = span[0];
                int value1 = registers[span[1]];
                int value2 = registers[span[2]];
                state.ProgramCounter += 3;
                registers[register] = value1 & value2;
                return vm;
            }
        }

        public class Or : Instruction.Instruction<Register> {
            public override byte OpCode { get; } = 0x07;
            public override IVirtualMachine<Register> Apply(IVirtualMachine<Register> vm) {
                var state = vm.State;
                var registers = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int register = span[0];
                int value1 = registers[span[1]];
                int value2 = registers[span[2]];
                state.ProgramCounter += 3;
                registers[register] = value1 | value2;
                return vm;
            }
        }

        public class Xor : Instruction.Instruction<Register> {
            public override byte OpCode { get; } = 0x08;
            public override IVirtualMachine<Register> Apply(IVirtualMachine<Register> vm) {
                var state = vm.State;
                var registers = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 3);
                int register = span[0];
                int value1 = registers[span[1]];
                int value2 = registers[span[2]];
                state.ProgramCounter += 3;
                registers[register] = value1 ^ value2;
                return vm;
            }
        }

        public class Not : Instruction.Instruction<Register> {
            public override byte OpCode { get; } = 0x09;
            public override IVirtualMachine<Register> Apply(IVirtualMachine<Register> vm) {
                var state = vm.State;
                var registers = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 2);
                int register = span[0];
                int value = registers[span[1]];
                state.ProgramCounter += 2;
                registers[register] = ~value;
                return vm;
            }
        }

        public class Jump : Instruction.Instruction<Register> {
            public override byte OpCode { get; } = 0x06;
            public override IVirtualMachine<Register> Apply(IVirtualMachine<Register> vm) {
                var state = vm.State;
                var registers = state.Holder;
                var span = state.Program.AsSpan(state.ProgramCounter, 1);
                int value = span[0];
                state.ProgramCounter = value;
                return vm;
            }
        }

        public class JumpIfZero : Instruction.Instruction<Register> {
            public override byte OpCode { get; } = 0x07;
            public override IVirtualMachine<Register> Apply(IVirtualMachine<Register> vm) {
                var state = vm.State;
                var registers = state.Holder;
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

        public class Halt : Instruction.Instruction<Register> {
            public override byte OpCode { get; } = 0xff;
            public override IVirtualMachine<Register> Apply(IVirtualMachine<Register> vm) {
                vm.State.ProgramCounter = vm.State.Program.Length;
                return vm;
            }
        }
    }

    public static class InstructionSet  
    {
        public static Instruction.Instruction<Register>[] Instructions = [
            new Instructions.Mov(),
            new Instructions.Add(),
            new Instructions.Sub(),
            new Instructions.Mul(),
            new Instructions.Div(),
            new Instructions.And(),
            new Instructions.Or(),
            new Instructions.Xor(),
            new Instructions.Not(),
            new Instructions.Jump(),
            new Instructions.JumpIfZero(),
            new Instructions.Halt()
        ];
    }


    public record Register(int Count) {
        public int[] Registers { get; set; } = new int[Count];
        public int this[int index] {
            get => Registers[index];
            set => Registers[index] = value;
        }

        public override string ToString() {
            return string.Join(", ", Registers);
        }
    }

    public record RegisterState() : IState<Register> {
        public Register Holder { get; set; } = new(8);
        public int ProgramCounter { get; set; } = 0;
        public int[] Memory { get; set; } = new int[1024];
        public byte[] Program { get; set; }

        public override string ToString() {
            return $"ProgramCounter: {ProgramCounter}, Registers: {Holder}";
        }
    }

    public record VirtualMachine : IVirtualMachine<Register> {
        public VirtualMachine(Instruction.Instruction<Register>[] instructionsSet) {
            int maxOpCode = instructionsSet.Max(i => i.OpCode);
            if(maxOpCode > 0xff) throw new Exception("Invalid OpCode");

            InstructionsSet = new Instruction.Instruction<Register>[maxOpCode + 1];
            foreach (var instruction in instructionsSet) {
                InstructionsSet[instruction.OpCode] = instruction;
            }
            State = new RegisterState();
        }
        public Instruction.Instruction<Register>[] InstructionsSet { get; set; }
        public IState<Register> State { get; set; }

        public override string ToString() {
            return State.ToString();
        }
    }
}