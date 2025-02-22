﻿using iLang.SyntaxDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using VirtualMachine.Example.Register;
using VirtualMachine.Instruction;

namespace VirtualMachine.iLang.Compilers
{
    class Opcode<T>(Instruction<T> instruction, Operand[] Operands)
    {
        public override string ToString() => $"{instruction.Name} {System.String.Join(" ", Operands.Select(x => x.ToString()))}";

        public Instruction<T> Op { get; } = instruction;
        public Operand[] Operands { get; set; } = Operands;
    }
    record Operand
    {
        public static implicit operator Operand(int value) => new Value(value);
        public static implicit operator Operand(string value) => new FunctionName(value);

        public static Operand None => new None();
    }
    record Value(int Number) : Operand
    {
        public override string ToString() => Number.ToString();
    }

    record None : Operand
    {
        public override string ToString() => "";
    }
    record FunctionName(string atom) : Operand
    {
        public override string ToString() => atom;
    }
    record Bytecode<TState>(List<Opcode<TState>> Instruction) 
    {
        public void Add(Instruction<TState> instruction) => Instruction.Add(new Opcode<TState>(instruction, []));
        public void Add(Instruction<TState> instruction, params Operand[] operands) => Instruction.Add(new Opcode<TState>(instruction, operands));
        public void Add(Instruction<TState> instruction, Operand operand) => Instruction.Add(new Opcode<TState>(instruction, [operand]));

        public void AddRange(Bytecode<TState> bytecode) => Instruction.AddRange(bytecode.Instruction);

        public void RemoveRange(int start, int count) => Instruction.RemoveRange(start, count);
        public void RemoveRange(int start) => Instruction.RemoveRange(start, Instruction.Count - start);

        // terribly bad code
        public int Pc(int index) => Instruction[..index].Sum(x => {
            // get Metadata attribute from instuction
            var metadata = x.Op.GetType().GetCustomAttribute<MetadataAttribute>();
            var opcodeSize = (metadata?.ImmediateSizes.Sum() ?? 0) + 1;
            return opcodeSize;
        });

        // terribly bad code
        public int Index(int pc) => Instruction
            .Select((x, index) => (Pc(index) == pc, index))
            .First(x => x.Item1)
            .index;
        public int Size => Instruction.Sum(x => {
            // get Metadata attribute from instuction
            var metadata = x.Op.GetType().GetCustomAttribute<MetadataAttribute>();
            var opcodeSize = (metadata?.ImmediateSizes.Sum() ?? 0) + 1;
            return opcodeSize;
        });

        public override string ToString() => string.Join("\n", Instruction.Select((x, i) => $"{new Bytecode<TState>(Instruction.Slice(0, i)).Size} : {x.ToString()}"));
    }
    internal class Context<T>(string name)
    {
        public string Name { get; set; } = name;
        public Bytecode<T> Bytecode { get; init; } = new(new List<Opcode<T>>());
        public Dictionary<string, int> Variables { get; init;  } = new();

        public Context<T> Snapshot
        {
            get
            {
                var newDict = new Dictionary<string, int>();

                foreach (var keyValuePair in Variables)
                {
                    newDict[keyValuePair.Key] = keyValuePair.Value;
                }

                var insutrctions = new List<Opcode<T>>();
                foreach (var opcode in this.Bytecode.Instruction)
                {
                    insutrctions.Add(new Opcode<T>(opcode.Op, opcode.Operands));
                }

                return new Context<T>(this.Name)
                {
                    Bytecode = new Bytecode<T>(insutrctions),
                    Variables = newDict
                };
            }
        }
    }

    internal static class Tools
    {
        public static string Mangle(string nameSpace, Identifier name)
        {
            if(System.String.IsNullOrEmpty(nameSpace)) return name.LocalName;
            return name.FullName;
        }

        public static int AbsoluteValue(int value) => value < 0 ? -value : value;
        public static int Address(string name) => AbsoluteValue(name.GetHashCode() % 1024);
    }

}
