using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualMachine.Example
{
    public static class Constants
    {
        public readonly static Range stackFrame = 0..512;
        public readonly static Range globalFrame = 513..1024;
        public readonly static int frameSize = 16;
    }
    public record SupportsCall
    {
        public Stack<int> Calls { get; set; } = new Stack<int>();
    }
}
