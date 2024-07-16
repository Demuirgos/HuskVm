using VirtualMachine.Instruction;

namespace VirtualMachine.Builder;

public static class AssemblyBuilder {
    public static byte[] Parse<T>(string code) {
        var tokens = code.Split(' ');
        var bytes = new List<byte>();

        var Instructions = InstructionSet<T>.Opcodes;
        if(Instructions.Any(i => i.Metadata == null)) throw new Exception($"Metadata is required, missing for some instructions {(String.Join(", ", Instructions.Where(i => i.Metadata == null).Select(i => i.Name)))}");
        var opcodes =  Instructions.ToDictionary(i => i.Name.ToLower());

        for(int i = 0; i < tokens.Length; i++) {
            var token = tokens[i].ToLower();
            if(opcodes.TryGetValue(token, out var instruction)) {
                
                bytes.Add(instruction.OpCode);
                foreach (var Immediate in instruction.Metadata.ImmediateSizes) {
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