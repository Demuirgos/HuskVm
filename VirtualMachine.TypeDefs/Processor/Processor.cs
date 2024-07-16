
using System;
using System.Linq;

namespace VirtualMachine.Processor
{
    public interface IVirtualMachine<T> {
        IVirtualMachine<T> LoadProgram(byte[] program);
        IVirtualMachine<T> Run();
        Instruction.Instruction<T>[] InstructionsSet { get; }
        IState<T> State { get; }
    }

    public interface IState<T> 
    {
        T Holder { get; }
        int ProgramCounter { get; set; }
        int[] Memory { get; }
        byte[] Program { get; set; }
    }

    public class BaseVirtualMachine<T> : IVirtualMachine<T> {
        public IVirtualMachine<T> LoadProgram(byte[] program) {
            State.Program = program;
            return this;
        }
        public IVirtualMachine<T> Run() {
            while (State.ProgramCounter < State.Program.Length) {
                var opCode = State.Program[State.ProgramCounter++];
                InstructionsSet[opCode].Apply(this);
            }
            return this;
        }
        protected BaseVirtualMachine(Instruction.Instruction<T>[] instructionsSet, IState<T> state) {
            int maxOpCode = instructionsSet.Max(i => i.OpCode);
            if(maxOpCode > 0xff) throw new Exception("Invalid OpCode");

            InstructionsSet = new Instruction.Instruction<T>[maxOpCode + 1];
            foreach (var instruction in instructionsSet) {
                InstructionsSet[instruction.OpCode] = instruction;
            }
            State = state;
        }

        public Instruction.Instruction<T>[] InstructionsSet { get; }
        public IState<T> State { get; set; }
    }
}