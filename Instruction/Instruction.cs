using VirtualMachine.Processor;

namespace VirtualMachine.Instruction
{
    public abstract class Instruction<T>
    {
        public abstract byte OpCode { get; }
        public abstract IVirtualMachine<T> Apply(IVirtualMachine<T> vm);
    }
}