using System;
using System.Collections.Generic;
using System.Linq;
using VirtualMachine.Instruction;

namespace VirtualMachine.Builder
{
    public class AssemblyBuilder<T>()
    {
        public List<byte> Bytecode { get; set; } = new List<byte>();
        public byte[] Build() => Bytecode.ToArray();
        public void LoadProgram(string bytecode) => Bytecode = new List<byte>(Parse(bytecode));

        public static byte[] Parse(string code) {
            var tokens = code.Split(' ');
            var bytes = new List<byte>();

            var Instructions = InstructionSet<T>.Opcodes;
            if(Instructions.Any(i => i.GetType().GetCustomAttributes(typeof(Instruction.MetadataAttribute), false).FirstOrDefault() is null)) throw new Exception($"Metadata is required");
            var opcodes =  Instructions.ToDictionary(i => i.Name.ToLower());

            for(int i = 0; i < tokens.Length; i++) {
                var token = tokens[i].ToLower();
                if(opcodes.TryGetValue(token, out var instruction)) {
                
                    bytes.Add(instruction.OpCode);
                    var metadata = instruction.GetType().GetCustomAttributes(typeof(Instruction.MetadataAttribute), false).FirstOrDefault() as MetadataAttribute;
                    foreach (var Immediate in metadata.ImmediateSizes) {
                        var value = tokens[++i];
                        if(Immediate == 1) bytes.Add(byte.Parse(value));
                        else if(Immediate == 2) bytes.AddRange(BitConverter.GetBytes(short.Parse(value)));
                        else if(Immediate == 4) {
                            bytes.AddRange(BitConverter.GetBytes(int.Parse(value)));
                        } else if (Immediate == 8) {
                            bytes.AddRange(BitConverter.GetBytes(long.Parse(value)));
                        } else throw new Exception("Invalid Immediate Size");
                    }
                } 
            }
            return bytes.ToArray();
        }
    }
}