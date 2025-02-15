using System;
using System.Linq;
using System.Reflection;
using VirtualMachine.Processor;

namespace VirtualMachine.Instruction
{
    public abstract class Instruction<T> 
    {
        public string Name => GetType().Name;
        public int Size
        {
            get
            {
                int count = 1;
                for (int k = 0; k < Metadata.ImmediateSizes.Length; k++)
                {
                    count += Metadata.ImmediateSizes[k];
                }
                return count;
            }
        }
        public abstract byte OpCode { get; }

        public MetadataAttribute Metadata => GetType().GetCustomAttribute<MetadataAttribute>();
        public abstract IVirtualMachine<T> Apply(IVirtualMachine<T> vm);
    }

    public static class InstructionSet<T>  
    {
        public static Instruction.Instruction<T>[] Opcodes {
            get {
                // get current assembly types not executing assembly
                var types = Assembly.GetAssembly(typeof(T))?.GetTypes()
                    .Where(t => t.BaseType?.IsGenericType == true && t.BaseType.GetGenericTypeDefinition() == typeof(Instruction.Instruction<>))
                    .Where(t => t.BaseType.GetGenericArguments()[0] == typeof(T))
                    .ToList();
                return types.Select(t => (Instruction.Instruction<T>)Activator.CreateInstance(t)).ToArray();
            }
        }
    }

    public class MetadataAttribute : Attribute {
        public MetadataAttribute(int argumentCount, int outputCount, params int[] immediateSizes) {
            ArgumentCount = argumentCount;
            OutputCount = outputCount;
            ImmediateSizes = immediateSizes;
        }
        public int ArgumentCount { get; set; }
        public int OutputCount { get; set; }
        public int[] ImmediateSizes { get; set; } = Array.Empty<int>();
    }
}