using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualMachine.Processor;

namespace VirtualMachine.iLang.Extras
{
    internal class Tracer<T> : ITracer<T>
    {
        public List<string> TraceLog { get; } = new List<string>();
        public void Trace(IVirtualMachine<T> vm)
        {
            var state = vm.State;
            var sb = new StringBuilder();
            sb.Append($"PC: {state.ProgramCounter} ");
            if(vm.State.ProgramCounter < vm.State.Program.Length)
            {
                var instruction = vm.InstructionsSet[state.Program[state.ProgramCounter]];
                sb.Append($"OP: {instruction.Name} ");
            }
            sb.Append($"MEM: {string.Join(", ", state.Memory)} ");
            sb.Append($"REG: {state.Holder} ");
            TraceLog.Add(sb.ToString());
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, TraceLog);
        }
    }
}
