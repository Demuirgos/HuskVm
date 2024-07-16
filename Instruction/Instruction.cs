using System.Reflection;
using VirtualMachine.Processor;

namespace VirtualMachine.Instruction;
public abstract class Instruction<T>
{
    public string Name => GetType().Name;
    public abstract byte OpCode { get; }
    public virtual Metadata Metadata { get; } 
    public abstract IVirtualMachine<T> Apply(IVirtualMachine<T> vm);
}

public static class InstructionSet<T>  
{
    public static Instruction.Instruction<T>[] Opcodes {
        get {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.BaseType?.IsGenericType == true && t.BaseType.GetGenericTypeDefinition() == typeof(Instruction.Instruction<>))
                .Where(t => t.BaseType.GetGenericArguments()[0] == typeof(T))
                .ToList();
            return types.Select(t => (Instruction.Instruction<T>)Activator.CreateInstance(t)).ToArray();
        }
    }
}

public record Metadata {
    public int ArgumentCount { get; init; }
    public int OutputCount { get; init; }
    public int[] ImmediateSizes { get; init; }
}