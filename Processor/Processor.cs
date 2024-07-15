using System.Buffers;

namespace VirtualMachine.Processor
{
    public interface IVirtualMachine<T> {
        IVirtualMachine<T> LoadProgram(byte[] program) {
            State.Program = program;
            return this;
        }
        IVirtualMachine<T> Run() {
            while (State.ProgramCounter < State.Program.Length) {
                var opCode = State.Program[State.ProgramCounter++];
                InstructionsSet[opCode].Apply(this);
            }
            return this;
        }
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
}