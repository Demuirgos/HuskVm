using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualMachine.Example;
using VirtualMachine.Processor;

namespace VirtualMachine.iLang.Extras
{
    internal class Tracer<T> : ITracer<T> where T : SupportsCall
    {
        public List<string> TraceLog { get; } = new List<string>();
        public void Trace(IVirtualMachine<T> vm)
        {
            string fixedLength<T>(T arg, int length)
            {
                string input = arg.ToString();
                if (input.Length > length)
                    return input.Substring(0, length);
                else
                    return input.PadRight(length, ' ');
            }
            var state = vm.State;
            var sb = new StringBuilder();

            sb.Append($"PC: {fixedLength(state.ProgramCounter, 5)} ");
            if(vm.State.ProgramCounter < vm.State.Program.Length)
            {
                var instruction = vm.InstructionsSet[state.Program[state.ProgramCounter]];
                sb.Append($"OP: {fixedLength(instruction.Name, 7)} ");
            }

            sb.Append($"CALLS: [{fixedLength(string.Join(", ", state.Holder.Calls), 10)}] ");

            sb.Append($"REG: {fixedLength(state.Holder, 30)} ");

            int stackFrameSize = 16;
            int stackCallSize = state.Holder.Calls.Count - 1;
            if(stackCallSize >= 0)
            {
                int[] stackFrame = state.Memory[(stackFrameSize * stackCallSize)..(stackFrameSize * stackCallSize + stackFrameSize)];
                sb.Append($"MEM: {fixedLength(string.Join(" ", stackFrame), 50)} ");
            }
            string trace = sb.ToString();

            Console.WriteLine(trace);

            TraceLog.Add(trace);
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, TraceLog);
        }
    }
}
