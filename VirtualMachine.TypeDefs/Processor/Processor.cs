
using System;
using System.Linq;
using VirtualMachine.TypeDefs.Processor;

namespace VirtualMachine.Processor
{
    public interface IVirtualMachine<T> {
        IVirtualMachine<T> LoadProgram(byte[] program);
        IVirtualMachine<T> Run<TTimer>(ITracer<T>? tracer = null, ITimer<TTimer>? timer = null);
        Instruction.Instruction<T>[] InstructionsSet { get; }
        IState<T> State { get; }
    }

    public interface ITracer<T>
    {
        void Trace(IVirtualMachine<T> vm);
    }

    public interface ITimer<T> : IDisposable 
    {
        T Resource { get; }
        void Start();
        void Restart();
        void Stop();
        void Reset();
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
        public IVirtualMachine<T> Run<TTimer>(ITracer<T> tracer, ITimer<TTimer>? timer) {
            tracer ??= NullTracer<T>.Instance;
            timer ??= NullTimer<TTimer>.Instance;
            timer.Start();
            tracer.Trace(this);
            while (State.ProgramCounter < State.Program.Length)
            {
                var opCode = State.Program[State.ProgramCounter++];
                InstructionsSet[opCode].Apply(this);
                tracer.Trace(this);
            }
            timer.Stop();
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